// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    eventLog.Append(Guid.NewGuid().ToString(), new ItemAddedToCart(
        new(Guid.NewGuid()),
        new(Guid.NewGuid()),
        1));
});

app.UseCratis();

app.Run();
