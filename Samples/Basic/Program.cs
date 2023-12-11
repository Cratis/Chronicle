using System;
// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Aggregates;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.UseCratis();
// builder.UseCratis(_ => _
//     .ForMicroservice("cd51c091-3bba-4608-87a8-93da1f88c4dd", "Basic Sample")
//     .MultiTenanted());

builder.Services.AddTransient<CartReducer>();
//builder.Services.AddTransient<OrderStateReducer>();
builder.Services.AddTransient<OrderStateProjection>();
var app = builder.Build();

app.MapGet("/add", () =>
{
    var eventLog = app.Services.GetRequiredService<IEventLog>();
    eventLog.Append(Guid.NewGuid().ToString(), new ItemAddedToCart(
        new(Guid.NewGuid()),
        new(Guid.NewGuid()),
        1,
        null));
});

app.MapGet("/agg", async () =>
{
    var aggregateRootFactory = app.Services.GetRequiredService<IAggregateRootFactory>();
    var eventSourceId = (EventSourceId)"299681c4-f100-4dea-bfea-633115349ed1";
    var order = await aggregateRootFactory.Get<Order>(eventSourceId);
    order.DoStuff();
    order.DoOtherStuff();
    await order.Commit();
});

app.UseCratis();

app.Run();
