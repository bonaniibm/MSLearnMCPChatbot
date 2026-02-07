# 📚 MS Learn MCP Chatbot

An interactive chatbot built with **ASP.NET Core Blazor Server** and **Azure AI Foundry Agent Service** that answers questions about Microsoft technologies using the official **Microsoft Learn MCP (Model Context Protocol) Server**.

> **Proof of Concept** — This is a .NET replication of the [Python notebook demo](https://github.com/retkowsky/Azure-AIGEN-demos/blob/main/MCP/MCP_Microsoft_Learn_Chatbot.ipynb) by Serge Retkowsky, rebuilt as a full web application using C# and .NET 9.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Azure AI Foundry](https://img.shields.io/badge/Azure%20AI-Foundry-0078D4?logo=microsoftazure)
![MCP](https://img.shields.io/badge/MCP-Model%20Context%20Protocol-purple)
![License](https://img.shields.io/badge/License-MIT-green)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Blazor Server App                      │
│  ┌──────────────┐   ┌──────────────────────────────┐    │
│  │  Chat UI      │──▸│  AgentService                │    │
│  │  (Blazor)     │   │  (Persistent Agents Client)  │    │
│  └──────────────┘   └───────────┬──────────────────┘    │
└─────────────────────────────────┼───────────────────────┘
                                  │ HTTPS
                                  ▼
┌─────────────────────────────────────────────────────────┐
│              Azure AI Foundry Agent Service               │
│  ┌──────────────────┐   ┌────────────────────────────┐  │
│  │  Persistent Agent │──▸│  MCP Tool Runtime          │  │
│  │  (GPT-4o-mini)    │   │  (microsoft_docs_search)   │  │
│  └──────────────────┘   └───────────┬────────────────┘  │
└─────────────────────────────────────┼───────────────────┘
                                      │ MCP Protocol
                                      ▼
                        ┌──────────────────────────┐
                        │  Microsoft Learn MCP      │
                        │  https://learn.microsoft  │
                        │  .com/api/mcp             │
                        └──────────────────────────┘
```

**How it works:**

1. User types a question in the Blazor chat UI.
2. The `AgentService` sends the question to an Azure AI Foundry **Persistent Agent**.
3. The agent uses the **MCP tool** (`microsoft_docs_search`) to search Microsoft Learn docs.
4. The agent synthesizes a response from the documentation and returns it.
5. The response is rendered as Markdown in the chat UI.

---

## ✅ Prerequisites

| Requirement | Details |
|---|---|
| **.NET 9 SDK** | [Download](https://dotnet.microsoft.com/download/dotnet/9.0) |
| **Azure Subscription** | [Free trial](https://azure.microsoft.com/free/) |
| **Azure AI Foundry Project** | With a deployed model (e.g., `gpt-4o-mini`, `gpt-4o`, `gpt-4.1-mini`) |
| **Azure CLI** | [Install](https://learn.microsoft.com/cli/azure/install-azure-cli) — used for `DefaultAzureCredential` |

---

## 🚀 Setup Instructions

### 1. Create an Azure AI Foundry Project

If you don't have one yet:

1. Go to [Azure AI Foundry](https://ai.azure.com/) (formerly Azure AI Studio).
2. Create a new **Project** (or use an existing one).
3. Deploy a model (e.g., `gpt-4o-mini` or `gpt-4.1-mini`).
4. Copy the **Project Endpoint** — it looks like:
   ```
   https://<your-resource>.services.ai.azure.com/api/projects/<project-id>
   ```

### 2. Authenticate with Azure

The app uses `DefaultAzureCredential`, so the simplest way is Azure CLI:

```bash
az login
```

Ensure you're logged into the correct subscription and tenant that has access to your AI Foundry project.

### 3. Clone and Configure

```bash
git clone https://github.com/<your-username>/mslearn-mcp-chatbot.git
cd mslearn-mcp-chatbot
```

Edit `src/MSLearnMCPChatbot/appsettings.json` and replace the placeholder:

```json
{
  "AzureAIFoundry": {
    "ProjectEndpoint": "https://<your-resource>.services.ai.azure.com/api/projects/<project-id>",
    "ModelDeploymentName": "gpt-4o-mini"
  }
}
```

> **Tip:** You can also use environment variables:
> ```bash
> export AzureAIFoundry__ProjectEndpoint="https://..."
> export AzureAIFoundry__ModelDeploymentName="gpt-4o-mini"
> ```

### 4. Run the Application

```bash
cd src/MSLearnMCPChatbot
dotnet run
```

Open your browser to **https://localhost:5001**.

---

## 🧩 Key Components

| File | Purpose |
|---|---|
| `Services/AgentService.cs` | Core service — creates Azure AI Foundry persistent agent with MCP tool, manages threads, sends messages, handles tool approvals |
| `Models/AzureAIFoundryOptions.cs` | Strongly-typed configuration for Azure AI Foundry settings |
| `Models/ChatMessage.cs` | Chat message model with role, content, tool call tracking |
| `Components/Pages/Home.razor` | Main Blazor chat page with full interactive UI |
| `Services/MarkdownService.cs` | Markdown-to-HTML rendering using Markdig |
| `Program.cs` | DI setup — registers AgentService as singleton |

---

## 🔧 Configuration Options

All options are under the `AzureAIFoundry` section in `appsettings.json`:

| Setting | Default | Description |
|---|---|---|
| `ProjectEndpoint` | *(required)* | Your Azure AI Foundry project endpoint URL |
| `ModelDeploymentName` | `gpt-4o-mini` | The model deployment name in your project |
| `McpServerUrl` | `https://learn.microsoft.com/api/mcp` | The MCP server URL |
| `McpServerLabel` | `microsoft_learn` | Unique label for the MCP server |
| `McpAllowedTools` | `["microsoft_docs_search"]` | List of allowed MCP tools |
| `RequireApproval` | `never` | Tool approval mode: `never`, `always`, or custom |

---

## 💡 How the MCP Integration Works

The **Model Context Protocol (MCP)** is an open standard that enables AI models to access external tools and data sources. In this app:

1. We create an `MCPToolDefinition` pointing to `https://learn.microsoft.com/api/mcp`.
2. This MCP server exposes the `microsoft_docs_search` tool that searches Microsoft Learn documentation.
3. When the agent receives a question, it decides to call `microsoft_docs_search` to find relevant docs.
4. The Azure AI Foundry service handles the MCP communication — your app doesn't need to run an MCP client.
5. The agent synthesizes the search results into a coherent answer.

This is the same approach as the [original Python notebook](https://github.com/retkowsky/Azure-AIGEN-demos/blob/main/MCP/MCP_Microsoft_Learn_Chatbot.ipynb), but using:
- **Azure.AI.Agents.Persistent** SDK (instead of `azure-ai-projects` Python SDK)
- **Blazor Server** web UI (instead of Jupyter notebook)

---

## 📁 Project Structure

```
mslearn-mcp-chatbot/
├── MSLearnMCPChatbot.sln
├── README.md
├── LICENSE
├── .gitignore
└── src/
    └── MSLearnMCPChatbot/
        ├── MSLearnMCPChatbot.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── Properties/
        │   └── launchSettings.json
        ├── Models/
        │   ├── AzureAIFoundryOptions.cs
        │   └── ChatMessage.cs
        ├── Services/
        │   ├── AgentService.cs
        │   └── MarkdownService.cs
        ├── Components/
        │   ├── _Imports.razor
        │   ├── App.razor
        │   ├── Routes.razor
        │   ├── Layout/
        │   │   └── MainLayout.razor
        │   └── Pages/
        │       ├── Home.razor
        │       └── Error.razor
        └── wwwroot/
            └── css/
                └── app.css
```

---

## 🔐 Authentication Notes

The app uses `DefaultAzureCredential` which tries multiple authentication methods in order:

1. **Environment variables** (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`)
2. **Azure CLI** (`az login`)  ← recommended for local dev
3. **Visual Studio / VS Code credential**
4. **Managed Identity** (when deployed to Azure)

For production, use **Managed Identity** by deploying to Azure App Service or Container Apps.

---

## 🚢 Deploy to Azure (Optional)

### Azure App Service

```bash
cd src/MSLearnMCPChatbot
dotnet publish -c Release -o ./publish

# Create and deploy
az webapp create --resource-group <rg> --plan <plan> --name mslearn-mcp-chatbot --runtime "DOTNET|9.0"
az webapp config appsettings set --resource-group <rg> --name mslearn-mcp-chatbot \
  --settings AzureAIFoundry__ProjectEndpoint="https://..."

cd publish && zip -r ../app.zip .
az webapp deployment source config-zip --resource-group <rg> --name mslearn-mcp-chatbot --src ../app.zip
```

Enable **System Managed Identity** and grant it the **Azure AI Developer** role on your AI Foundry project.

---

## ⚠️ Known Limitations (PoC)

- **No streaming** — responses are returned in full after the agent completes (polling-based).
- **No persistent chat history** — conversations reset on browser refresh.
- **Single agent instance** — all users share one agent (threads are per-session).
- **No authentication** — no user auth on the web app itself.
- **Error handling is basic** — intended for demo/PoC use.

---

## 🤝 Contributing

This is an open-source proof of concept. Contributions welcome!

1. Fork the repo
2. Create a feature branch
3. Submit a PR

---

## 📖 References

- [Azure AI Foundry Agent Service - MCP Tool](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/model-context-protocol)
- [MCP Tool Code Samples (C# / Python / REST)](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/model-context-protocol-samples)
- [Using MCP with Foundry Agents](https://learn.microsoft.com/en-us/agent-framework/user-guide/model-context-protocol/using-mcp-with-foundry-agents)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Original Python Notebook (Serge Retkowsky)](https://github.com/retkowsky/Azure-AIGEN-demos/blob/main/MCP/MCP_Microsoft_Learn_Chatbot.ipynb)
- [Build Agents using MCP on Azure](https://learn.microsoft.com/en-us/azure/developer/ai/intro-agents-mcp)

---

## 📝 License

MIT — see [LICENSE](LICENSE).
