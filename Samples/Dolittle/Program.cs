// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Sample;

var builder = Host.CreateDefaultBuilder()
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .ConfigureContainer<ContainerBuilder>(containerBuilder => containerBuilder.RegisterDefaults(Startup.Types))
                    .ConfigureWebHostDefaults(_ => _.UseStartup<Startup>());

var app = builder.Build();

app.Run();

/*
var builder = WebApplication.CreateBuilder(args)
                            .UseCratis()
                            .UseCratisWorkbench();
var app = builder.Build();
app.UseCratisWorkbench();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Run();
*/
