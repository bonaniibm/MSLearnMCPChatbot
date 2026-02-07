namespace MSLearnMCPChatbot.Models;

/// <summary>
/// Configuration for Azure AI Foundry project and MCP server connection.
/// </summary>
public class AzureAIFoundryOptions
{
    public const string SectionName = "AzureAIFoundry";

    /// <summary>
    /// Azure AI Foundry project endpoint URL.
    /// Example: "https://your-project.services.ai.azure.com/api/projects/your-project-id"
    /// </summary>
    public string ProjectEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Model deployment name (e.g., "gpt-4o-mini", "gpt-4o", "gpt-4.1-mini").
    /// </summary>
    public string ModelDeploymentName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// The MCP server URL. Defaults to the Microsoft Learn MCP server.
    /// </summary>
    public string McpServerUrl { get; set; } = "https://learn.microsoft.com/api/mcp";

    /// <summary>
    /// A unique label for the MCP server instance.
    /// </summary>
    public string McpServerLabel { get; set; } = "microsoft_learn";

    /// <summary>
    /// Optional list of allowed tool names from the MCP server.
    /// </summary>
    public List<string> McpAllowedTools { get; set; } = ["microsoft_docs_search"];

    /// <summary>
    /// Approval mode for MCP tool calls: "never", "always", or custom JSON.
    /// </summary>
    public string RequireApproval { get; set; } = "never";
}
