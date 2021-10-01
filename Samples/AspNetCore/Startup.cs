// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AspNetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCratisWorkbench();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseCratisWorkbench();
        }
    }
}
