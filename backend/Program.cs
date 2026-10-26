using ContentManager.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();

var app = builder.Build();

if (await app.TryRunOfflineCommandAsync(args))
    return;

app.UseApplicationPipeline();
app.MapApplicationEndpoints();
app.Run();
