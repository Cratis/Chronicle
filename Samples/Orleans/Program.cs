// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Workbench.Embedded;
using Cratis.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

builder.UseCratisMongoDB(
    mongo =>
    {
        mongo.Server = "mongodb://localhost:27017";
        mongo.Database = "orleans";
    });

builder.Host.UseDefaultServiceProvider(_ => _.ValidateOnBuild = false);

builder.Services.AddSingleton(Globals.JsonSerializerOptions);
builder.Services.AddControllers();
builder.AddCratisChronicle();

builder.Host.UseOrleans(silo =>
    {
        silo
            .UseDashboard(options =>
            {
                options.Host = "*";
                options.Port = 8081;
                options.HostSelf = true;
            })
            .UseCratisChronicleWorkbench()
            .UseLocalhostClustering()
            .AddCratisChronicle(
                options => options.EventStoreName = "sample",
                _ => _.WithMongoDB());
    })
    .UseConsoleLifetime();

var app = builder.Build();
var f = app.Services.GetRequiredService<IMongoDBClientFactory>();
app.UseCratisChronicle();
app.MapControllers();

await app.RunAsync();
