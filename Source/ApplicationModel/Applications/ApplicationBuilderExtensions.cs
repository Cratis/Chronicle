// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio;
using Aksio.Execution;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for the application builder.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Use Aksio default setup.
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
        /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
        public static IApplicationBuilder UseAksio(this IApplicationBuilder app)
        {
            Internals.ServiceProvider = app.ApplicationServices;

            app.UseWebSockets();

            if (RuntimeEnvironment.IsDevelopment)
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Swagger"));
            }

            app.UseDefaultLogging();

            app.UseCratis();

            return app;
        }
    }
}
