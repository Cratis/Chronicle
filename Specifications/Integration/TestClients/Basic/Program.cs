// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication
                .CreateBuilder(args)
                .UseCratis();

builder.Host.UseMongoDB();

var app = builder.Build();
app.UseRouting();
app.UseCratis();

app.Run();
