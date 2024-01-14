using Microsoft.EntityFrameworkCore;
using Pastebin;
using Pastebin.Database;
using Pastebin.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddServices(builder.Configuration);
var app = builder.Build();

using var serviceScope = app.Services.GetService<IServiceScopeFactory>()?.CreateScope();
var context = serviceScope!.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await context.Database.MigrateAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGroup("/").MapS3Keys();

app.Run();