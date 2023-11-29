// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Client;
using Aksio.Cratis.Configuration;
using Aksio.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<KernelConnectivity>(_ => _.SingleKernel = new() { Endpoint = new Uri("http://cratis:8080") });
builder.UseCratis(_ => _
    .ForMicroservice(MicroserviceId.Unspecified, "Basic"));
var app = builder.Build();
app.UseCratis();
await app.RunAsync();
