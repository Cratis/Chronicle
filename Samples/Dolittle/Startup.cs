// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Dolittle.Schemas;
using Cratis.Types;

namespace Sample
{
    public class Startup
    {
        internal static readonly ITypes Types = new Types();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddSingleton(Types);
            services.AddSingleton(new SchemaStoreConfiguration("localhost", 27017));

            services.UseCratisWorkbench();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.AddCratisWorkbench();

            app
                .ApplicationServices
                .GetService<ISchemaStore>()?
                .DiscoverGenerateAndConsolidate();
        }
    }
}
