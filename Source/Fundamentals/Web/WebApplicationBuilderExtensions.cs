// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders.Physical;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents extension methods for building on <see cref="IApplicationBuilder"/>.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Run as a single page application - typically end off your application configuration in Startup.cs with this.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> you're building.</param>
    /// <param name="options">Optional <see cref="SharedOptions"/> to file that will be sent as the single page.</param>
    /// <remarks>
    /// If there is no <see cref="SharedOptions"/> given, it will default to index.html inside your wwwwroot of the content root.
    /// </remarks>
    public static void RunAsSinglePageApplication(this IApplicationBuilder app, SharedOptions? options = null)
    {
        var environment = app.ApplicationServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
        var fileInfo = options?.FileProvider?.GetFileInfo("index.html") ?? new PhysicalFileInfo(new FileInfo($"{environment!.ContentRootPath}/wwwroot/index.html"));

        app.Run(async context =>
        {
            if (Path.HasExtension(context.Request.Path))
            {
                return;
            }
            context.Request.Path = options?.RequestPath ?? new PathString("/");

            if (File.Exists(fileInfo.PhysicalPath))
            {
                await context.Response.SendFileAsync(fileInfo);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        });
    }
}
