// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Dolittle.Workbench;
using Cratis.Types;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Sample
{
    public class Startup
    {
        internal static readonly ITypes Types = new Types();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddControllers()
                    .PartManager
                    .ApplicationParts
                    .Add(new AssemblyPart(typeof(Startup).Assembly));
            services.AddSingleton(Types)
                .AddProjections()
                .AddDolittleEventTypes()
                .AddDolittleSchemas()
                .AddDolittleProjections()
                .AddCratisWorkbench(_ => _.UseDolittle());
        }

        public void Configure(IApplicationBuilder app)
        {
            app
                .UseRouting()
                .UseDolittleSchemas()
                .UseDolittleProjections()
                .UseCratisWorkbench();
        }
    }
}
