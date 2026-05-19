using Altinn.ApiClients.Dan.Extensions;
using Altinn.ApiClients.Dan.Models;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>("my-client-definition-for-dan", 
    builder.Configuration.GetSection("MaskinportenSettings"));
builder.Services
    .AddDanClient(builder.Configuration.GetSection("DanSettings"), conf => new DanConfiguration
    {
        Deserializer = new JsonNetDeserializer()
    })
    .AddMaskinportenHttpMessageHandler<SettingsJwkClientDefinition>("my-client-definition-for-dan");

builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();