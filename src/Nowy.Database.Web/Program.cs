using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;
using Nowy.Database.Server;
using Nowy.Database.Server.Endpoints;
using Nowy.Database.Server.Services;
using Nowy.MessageHub.Client;
using Nowy.Standard;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddNowyStandard();
builder.Services.AddNowyDatabaseServer(config => { });
builder.Services.AddNowyMessageHubClient(config =>
{
    // config.AddEndpoint("https://main.messagehub.schulz.dev");
    // config.AddEndpoint("https://main.messagehub.nowykom.de");

});


WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // app.UseExceptionHandler("/Error");
    app.UseDeveloperExceptionPage();
}

app.UseNowyDatabaseServer();

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nowy Database"); });

app.Run();
