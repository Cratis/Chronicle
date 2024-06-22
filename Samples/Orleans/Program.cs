// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Configuration;
using Cratis.Json;
using Cratis.MongoDB;
using Orleans.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.UseMongoDB(_ => _
    .WithStaticServer("mongodb://localhost:27017")
    .WithStaticDatabaseName("orleans"));

builder.Services.AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>();
builder.Services.AddSingleton(new Storage { Type = "mongodb" });
builder.Host.UseDefaultServiceProvider(_ => _.ValidateOnBuild = false);

builder.Services.AddSingleton(Globals.JsonSerializerOptions);

builder.Host.UseOrleans(silo =>
    {
        silo
            .UseLocalhostClustering()
            .AddChronicle()
            .UseMongoDB();

        silo.Services.AddSerializer(serializerBuilder => serializerBuilder.AddJsonSerializer(
            _ => _ == typeof(JsonObject) || (_.Namespace?.StartsWith("Cratis") ?? false),
            Globals.JsonSerializerOptions));
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
