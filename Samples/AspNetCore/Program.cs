// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//new HostBuilder().ConfigureWebHostDefaults()

var builder = WebApplication.CreateBuilder(args)
                            .UseCratis()
                            .UseCratisWorkbench();
var app = builder.Build();
app.AddCratisWorkbench();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Run();
