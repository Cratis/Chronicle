// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AspNetCore;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;

var builder = WebApplication.CreateBuilder(args)
    .AddCratisChronicle(options =>
    {
        options.EventStore = "AspNetCoreTestApp";
        options.WithCamelCaseNamingPolicy();
    });
var app = builder.Build();
app.UseCratisChronicle();

app.MapGet("/", async (IEventLog eventLog) => await eventLog.Append(Guid.NewGuid(), new MyEvent()));

await app.RunAsync();
