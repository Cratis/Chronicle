// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Basic;

using var client = new CratisClient("cratis://localhost:35000");
var eventStore = client.GetEventStore("cd51c091-3bba-4608-87a8-93da1f88c4dd");
await eventStore.Observers.RegisterKnownObservers();
await eventStore.EventLog.Append(
    "299681c4-f100-4dea-bfea-633115349ed1",
    new ItemAddedToCart(
        new(Guid.NewGuid()),
        new(Guid.NewGuid()),
        1));

// GrpcClientFactory.AllowUnencryptedHttp2 = true;
// using var channel = GrpcChannel.ForAddress("http://localhost:35000");
// var eventSequences = channel.CreateGrpcService<IEventSequences>();
// var result = await eventSequences.Append(new()
// {
//     MicroserviceId = Guid.Empty.ToString(),
//     TenantId = Guid.Empty,
//     EventSequenceId = EventSequenceId.Log.ToString(),
//     EventSourceId = Guid.NewGuid().ToString(),
//     Content = "{}",
//     Causation = Enumerable.Empty<Causation>(),
//     EventType = new()
//     {
//         Id = "123",
//         Generation = 1
//     },
//     Identity = new()
//     {
//         Subject = "123",
//         Name = "Horse",
//         UserName = "horse"
//     }
// });


/*
var builder = WebApplication.CreateBuilder(args);
//builder.UseCratis();
builder.UseCratis(_ => _
    .ForMicroservice("cd51c091-3bba-4608-87a8-93da1f88c4dd", "Basic Sample")
    .MultiTenanted());

builder.Services.AddTransient<CartReducer>();
var app = builder.Build();

app.MapGet("/add", () =>
{
    var eventLog = app.Services.GetRequiredService<IEventLog>();
    eventLog.Append("299681c4-f100-4dea-bfea-633115349ed1", new ItemAddedToCart(
        new(Guid.NewGuid()),
        new(Guid.NewGuid()),
        1));
});

app.UseCratis();

app.Run();
*/
