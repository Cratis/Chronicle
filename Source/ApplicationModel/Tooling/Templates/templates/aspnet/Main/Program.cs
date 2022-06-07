// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Sample;

var builder = Host.CreateDefaultBuilder()
                    .UseAksio(microserviceId: "0e1219ec-7136-40d8-a6e6-99c05ba22a30")
                    .ConfigureWebHostDefaults(_ => _.UseStartup<Startup>());

var app = builder.Build();

app.Run();
