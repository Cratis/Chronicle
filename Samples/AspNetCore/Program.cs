// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

var builder = WebApplication.CreateBuilder(args);
builder.AddCratisChronicle(options => options.EventStore = "AspNetCore");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.UseCratisChronicle();

await app.RunAsync();
