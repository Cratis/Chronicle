// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Represents extension methods for building on <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Add the Cratis API endpoints for Workbench specific for Dolittle.
        /// /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/> to add to.</param>
        /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
        public static IApplicationBuilder AddCratisWorkbench(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseRouting();
            var filesOptions = new SharedOptions
            {
                RequestPath = "/events",
                FileProvider = new EmbeddedFileProvider(
                    typeof(Root).Assembly,
                    $"{typeof(Root).Namespace}.workbench")
            };

            applicationBuilder.UseDefaultFiles(new DefaultFilesOptions(filesOptions));
            applicationBuilder.UseStaticFiles(new StaticFileOptions(filesOptions));

            applicationBuilder.UseEndpoints(_ => _.MapControllers());
            applicationBuilder.RunAsSinglePageApplication(filesOptions);

            return applicationBuilder;
        }

        /// <summary>
        /// Run as a single page application - typically end off your application configuration in Startup.cs with this.
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/> you're building.</param>
        /// <param name="options">Optional <see cref="SharedOptions"/> to file that will be sent as the single page.</param>
        /// <remarks>
        /// If there is no <see cref="SharedOptions"/> given, it will default to index.html inside your wwwwroot of the content root.
        /// </remarks>
        public static void RunAsSinglePageApplication(this IApplicationBuilder app, SharedOptions? options)
        {
            var environment = app.ApplicationServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            var fileInfo = options?.FileProvider?.GetFileInfo("index.html") ?? new PhysicalFileInfo(new FileInfo($"{environment!.ContentRootPath}/wwwroot/index.html"));

            app.Run(async context =>
            {
                if (Path.HasExtension(context.Request.Path)) await Task.CompletedTask.ConfigureAwait(false);
                context.Request.Path = options?.RequestPath ?? new PathString("/");

                await context.Response.SendFileAsync(fileInfo).ConfigureAwait(false);
            });
        }
    }
}
