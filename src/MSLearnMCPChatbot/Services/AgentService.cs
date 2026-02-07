using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Options;
using MSLearnMCPChatbot.Models;
using System.Collections.Concurrent;

namespace MSLearnMCPChatbot.Services;

/// <summary>
/// Service that manages Azure AI Foundry Persistent Agents with MCP tool integration.
/// Each chat session gets its own thread; a single agent is reused across sessions.
/// </summary>
public class AgentService : IDisposable
{
    private readonly AzureAIFoundryOptions _options;
    private readonly ILogger<AgentService> _logger;
    private readonly PersistentAgentsClient _client;

    private PersistentAgent? _agent;
    private readonly SemaphoreSlim _agentLock = new(1, 1);

    // Cache thread objects for the CreateRunAsync overload that accepts ToolResources
    private readonly ConcurrentDictionary<string, PersistentAgentThread> _threads = new();

    private const string AgentName = "MSLearnMCPChatbot";
    private const string AgentInstructions = """
        You are a helpful AI assistant specialized in Microsoft technologies.
        You answer questions by searching the Microsoft Learn documentation using the MCP tools available to you.
        
        Guidelines:
        - Always use the microsoft_docs_search tool to find relevant documentation before answering.
        - Provide accurate, well-structured answers based on official Microsoft documentation.
        - Include relevant links to Microsoft Learn pages when possible.
        - If the documentation doesn't cover a topic, clearly state that.
        - Format responses using Markdown for readability.
        - Be concise but thorough.
        """;

    public AgentService(IOptions<AzureAIFoundryOptions> options, ILogger<AgentService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _client = new PersistentAgentsClient(
        _options.ProjectEndpoint,
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Pin to the tenant where your Azure AI Foundry resource lives
            TenantId = "fcf67057-50c9-4ad4-98f3-ffca64add9e9"
        }));
    }

    /// <summary>
    /// Ensures the persistent agent is created (once) and returns it.
    /// </summary>
    private async Task<PersistentAgent> GetOrCreateAgentAsync()
    {
        if (_agent is not null) return _agent;

        await _agentLock.WaitAsync();
        try
        {
            if (_agent is not null) return _agent;

            _logger.LogInformation("Creating persistent agent with MCP tool...");

            // Create the MCP tool definition pointing to Microsoft Learn
            var mcpTool = new MCPToolDefinition(
                serverLabel: _options.McpServerLabel,
                serverUrl: _options.McpServerUrl);

            foreach (var tool in _options.McpAllowedTools)
            {
                mcpTool.AllowedTools.Add(tool);
            }

            _agent = await _client.Administration.CreateAgentAsync(
                model: _options.ModelDeploymentName,
                name: AgentName,
                instructions: AgentInstructions,
                tools: [mcpTool]);

            _logger.LogInformation("Agent created: {AgentId}", _agent.Id);
            return _agent;
        }
        finally
        {
            _agentLock.Release();
        }
    }

    /// <summary>
    /// Creates a new conversation thread and returns its ID.
    /// IMPORTANT: Do NOT pass ToolResources here — those go on the Run, not the Thread.
    /// </summary>
    public async Task<string> CreateThreadAsync()
    {
        // Simple thread creation with NO parameters
        var response = await _client.Threads.CreateThreadAsync();
        var thread = response.Value;

        // Cache the thread object — needed for CreateRunAsync overload with ToolResources
        _threads[thread.Id] = thread;

        _logger.LogInformation("Thread created: {ThreadId}", thread.Id);
        return thread.Id;
    }

    /// <summary>
    /// Sends a user message to the given thread, runs the agent, and returns the assistant's response.
    /// </summary>
    public async Task<ChatMessage> SendMessageAsync(string threadId, string userMessage)
    {
        var agent = await GetOrCreateAgentAsync();

        // Add user message to the thread
        await _client.Messages.CreateMessageAsync(
            threadId,
            MessageRole.User,
            userMessage);

        // Configure MCP tool resources
        var mcpToolResource = new MCPToolResource(_options.McpServerLabel);
        var toolResources = mcpToolResource.ToToolResources();

        // Get the cached thread object for the overload that accepts ToolResources
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            // Fallback: fetch the thread if not cached
            var threadResponse = await _client.Threads.GetThreadAsync(threadId);
            thread = threadResponse.Value;
            _threads[threadId] = thread;
        }

        // Create run using the (PersistentAgentThread, PersistentAgent, ToolResources) overload
        var run = await _client.Runs.CreateRunAsync(thread, agent, toolResources);
        _logger.LogInformation("Run created: {RunId}, Status: {Status}", run.Value.Id, run.Value.Status);

        var toolCallsUsed = new List<string>();

        // Poll until the run completes
        while (run.Value.Status == RunStatus.Queued
            || run.Value.Status == RunStatus.InProgress
            || run.Value.Status == RunStatus.RequiresAction)
        {
            await Task.Delay(1000);
            run = await _client.Runs.GetRunAsync(threadId, run.Value.Id);

            // Handle tool approval if required
            if (run.Value.Status == RunStatus.RequiresAction
                && run.Value.RequiredAction is SubmitToolApprovalAction toolApprovalAction)
            {
                var toolApprovals = new List<ToolApproval>();

                foreach (var toolCall in toolApprovalAction.SubmitToolApproval.ToolCalls)
                {
                    if (toolCall is RequiredMcpToolCall mcpToolCall)
                    {
                        _logger.LogInformation("Auto-approving MCP tool call: {ToolName}", mcpToolCall.Name);
                        toolCallsUsed.Add(mcpToolCall.Name);

                        toolApprovals.Add(new ToolApproval(mcpToolCall.Id, approve: true));
                    }
                }

                if (toolApprovals.Count > 0)
                {
                    run = await _client.Runs.SubmitToolOutputsToRunAsync(
                        threadId, run.Value.Id, toolApprovals: toolApprovals);
                }
            }
        }

        _logger.LogInformation("Run completed: {Status}", run.Value.Status);

        if (run.Value.Status == RunStatus.Failed)
        {
            var errorMsg = run.Value.LastError?.Message ?? "Unknown error";
            _logger.LogError("Run failed: {Error}", errorMsg);
            return new ChatMessage
            {
                Role = "assistant",
                Content = $"⚠️ Sorry, something went wrong: {errorMsg}",
                ToolCallsUsed = toolCallsUsed
            };
        }

        // Retrieve the assistant's latest response
        var messages = _client.Messages.GetMessages(
            threadId,
            order: ListSortOrder.Descending,
            limit: 1);

        foreach (var msg in messages)
        {
            if (msg.Role == MessageRole.Agent)
            {
                var content = string.Join("\n", msg.ContentItems
                    .OfType<MessageTextContent>()
                    .Select(c => c.Text));

                return new ChatMessage
                {
                    Role = "assistant",
                    Content = content,
                    ToolCallsUsed = toolCallsUsed
                };
            }
        }

        return new ChatMessage
        {
            Role = "assistant",
            Content = "I couldn't generate a response. Please try again.",
            ToolCallsUsed = toolCallsUsed
        };
    }

    /// <summary>
    /// Deletes a conversation thread.
    /// </summary>
    public async Task DeleteThreadAsync(string threadId)
    {
        try
        {
            _threads.TryRemove(threadId, out _);
            await _client.Threads.DeleteThreadAsync(threadId);
            _logger.LogInformation("Thread deleted: {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete thread {ThreadId}", threadId);
        }
    }

    public void Dispose()
    {
        if (_agent is not null)
        {
            try
            {
                _client.Administration.DeleteAgent(_agent.Id);
                _logger.LogInformation("Agent deleted: {AgentId}", _agent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete agent on dispose");
            }
        }

        _agentLock.Dispose();
        GC.SuppressFinalize(this);
    }
}