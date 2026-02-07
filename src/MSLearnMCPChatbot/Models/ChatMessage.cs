namespace MSLearnMCPChatbot.Models;

/// <summary>
/// Represents a single message in the chat conversation.
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsStreaming { get; set; } = false;
    public List<string> ToolCallsUsed { get; set; } = [];

    public bool IsUser => Role.Equals("user", StringComparison.OrdinalIgnoreCase);
    public bool IsAssistant => Role.Equals("assistant", StringComparison.OrdinalIgnoreCase);
    public bool IsSystem => Role.Equals("system", StringComparison.OrdinalIgnoreCase);
}
