// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Sample;

var builder = Host.CreateDefaultBuilder()
                    .UseAksio(microserviceId: "40dda9cf-38cc-4bf5-a249-ba3ce4f8861f")
                    .ConfigureWebHostDefaults(_ => _.UseStartup<Startup>());

var app = builder.Build();

app.Run();
