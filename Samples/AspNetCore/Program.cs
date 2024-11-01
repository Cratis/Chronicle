// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Shared.Carts;
using Shared.Orders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<OrderStateProjection>();
builder.Services.AddTransient<OrderStateReducer>();
builder.Services.AddTransient<OrderReactor>();
builder.Services.AddTransient<CartProjection>();
builder.Services.AddTransient<CartReactor>();
builder.Services.AddTransient<CartReducer>();

builder.AddCratisChronicle(options => options.EventStore = "AspNetCore");

var app = builder.Build();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(options => options.InjectStylesheet("/swagger-ui/SwaggerDark.css"));
app.UseCratisChronicle();

await app.RunAsync();
