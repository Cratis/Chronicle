// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Basic;

using var client = new CratisClient("cratis://localhost:35000");
var eventStore = client.GetEventStore("cd51c091-3bba-4608-87a8-93da1f88c4dd");
await eventStore.Observers.RegisterKnownObservers();
await eventStore.EventLog.Append(
    eventSourceId: Guid.NewGuid(),
    new ItemAddedToCart(
        PersonId: new(Guid.NewGuid()),
        MaterialId: new(Guid.NewGuid()),
        Quantity: 1));

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
