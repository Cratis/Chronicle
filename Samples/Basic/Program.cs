// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
//builder.UseCratis(_ => _.MultiTenanted());
builder.UseCratis(_ => _.MultiTenanted());
var app = builder.Build();
app.UseCratis();

app.MapGet("/", () =>
{
    var eventLog = app.Services.GetRequiredService<IEventLog>();
    eventLog.Append("49b9727f-64da-4d5d-bb52-8a3fc77f6a81", new MyEvent());
});

app.Run();
