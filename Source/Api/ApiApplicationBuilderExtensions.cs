// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using Cratis.Execution;

namespace Cratis.Api.Server;

/// <summary>
/// Holds the extension methods for configuring the Api for the <see cref="IApplicationBuilder"/> type.
/// </summary>
public static class ApiApplicationBuilderExtensions
{
    /// <summary>
    /// Use the Cratis Chronicle API.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratisChronicleApi(this IApplicationBuilder app)
    {
        if (RuntimeEnvironment.IsDevelopment)
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCratisApplicationModel();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        app.UseMicrosoftIdentityPlatformIdentityResolver();
        app.UseSwagger();
        app.UseSwaggerUI(options => options.InjectStylesheet("/swagger-ui/SwaggerDark.css"));
        return app;
    }
}
