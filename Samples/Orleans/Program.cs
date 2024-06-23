// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Json;
using Cratis.MongoDB;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.UseMongoDB(_ => _
    .WithStaticServer("mongodb://localhost:27017")
    .WithStaticDatabaseName("orleans"));

builder.Host.UseDefaultServiceProvider(_ => _.ValidateOnBuild = false);

builder.Services.AddSingleton(Globals.JsonSerializerOptions);

builder.Host.UseOrleans(silo =>
    {
        silo
            .UseLocalhostClustering()
            .AddChronicle(_ => _
                .WithMongoDB());
    })
    .UseConsoleLifetime();

var app = builder.Build();
var f = app.Services.GetRequiredService<IMongoDBClientFactory>();

app.MapGet(
    "/",
    (IGrainFactory grains) =>
    {
        var grain = grains.GetGrain<IMyGrain>(Guid.NewGuid());
        return grain.DoStuff();
    });

await app.RunAsync();
