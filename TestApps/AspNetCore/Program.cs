// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AspNetCore;
using Cratis.Chronicle;

var builder = WebApplication.CreateBuilder(args)
    .AddCratisChronicle(
        options => options.EventStore = "AspNetCoreTestApp",
        configure: chronicle => chronicle.WithCamelCaseNamingPolicy());

builder.Services.AddSingleton<ReactorInvocationLog>();

var app = builder.Build();
app.UseCratisChronicle();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapTestAppApi();

await app.RunAsync();
