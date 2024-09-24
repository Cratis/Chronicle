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
    public static WebApplication UseCratisChronicleApi(this WebApplication app)
    {
        if (RuntimeEnvironment.IsDevelopment)
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseWebSockets();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
        app.UseSwagger();
        app.UseSwaggerUI(options => options.InjectStylesheet("/swagger-ui/SwaggerDark.css"));
        app.UseCratisApplicationModel();
        app.MapFallbackToFile("/index.html");

        return app;
    }
}
