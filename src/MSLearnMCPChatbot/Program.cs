using MSLearnMCPChatbot.Models;
using MSLearnMCPChatbot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Bind configuration
builder.Services.Configure<AzureAIFoundryOptions>(
    builder.Configuration.GetSection(AzureAIFoundryOptions.SectionName));

// Register the Agent Service as a singleton (one agent, many threads)
builder.Services.AddSingleton<AgentService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<MSLearnMCPChatbot.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
