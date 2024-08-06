// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Chronicle.Setup;
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
builder.UseCratisChronicle();

builder.Host.UseOrleans(silo =>
    {
        silo
            .UseDashboard(options =>
            {
                options.Host = "*";
                options.Port = 8081;
                options.HostSelf = true;
            })
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
app.MapGet(
    "/",
    async (IAggregateRootFactory aggregateRootFactory) =>
    {
        var aggregateRoot = await aggregateRootFactory.Get<IOrder>("6fbd1b71-923d-4fa7-bf44-777dcb091218");
        await aggregateRoot.DoStuff();
    });

app.MapGet(
    "/users",
    async (IEventLog eventLog) =>
    {
        var userId = Guid.Parse("3444635c-8174-47b3-99dd-a27cd3ea80e4");
        var groupId = Guid.NewGuid();

        var result = await eventLog.Append(userId, new Events.Users.OnboardingStarted("My User", "asdasd", "asdasdasd"));
        result = await eventLog.Append(userId, new Events.Users.PasswordChanged("awesome"));
        result = await eventLog.Append(groupId, new Events.Groups.GroupAdded("My Group"));
        result = await eventLog.Append(groupId, new Events.Groups.UserAddedToGroup(userId));
        result = await eventLog.Append(userId, new Events.Users.OnboardingCompleted());
    });

app.MapGet(
    "/users/rename",
    async (IEventLog eventLog) =>
    {
        var userId = Guid.Parse("3444635c-8174-47b3-99dd-a27cd3ea80e4");
        var groupId = Guid.NewGuid();

        var result = await eventLog.Append(userId, new Events.Users.UserNameChanged("My User"));
    });

app.MapGet(
    "/users/new",
    async (IEventLog eventLog) =>
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var result = await eventLog.Append(userId, new Events.Users.UserNameChanged("My User"));
    });


await app.RunAsync();
