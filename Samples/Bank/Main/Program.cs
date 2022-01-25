// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Sample;

var builder = Host.CreateDefaultBuilder()
                    .UseAksio()
                    .ConfigureWebHostDefaults(_ => _.UseStartup<Startup>());

var app = builder.Build();

app.Run();
