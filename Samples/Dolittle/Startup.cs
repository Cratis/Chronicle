// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Dolittle.Workbench;
using Cratis.Types;

namespace Sample
{
    public class Startup
    {
        internal static readonly ITypes Types = new Types();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddSingleton(Types)
                .AddDolittleEventTypes()
                .AddDolittleSchemaStore("localhost", 27017)
                .AddProjections()
                .AddDolittleProjections()
                .AddCratisWorkbench(_ => _.UseDolittle());
        }

        public void Configure(IApplicationBuilder app)
        {
            app
                .UseRouting()
                .UseDolittleSchemaStore()
                .UseCratisWorkbench();
        }
    }
}
