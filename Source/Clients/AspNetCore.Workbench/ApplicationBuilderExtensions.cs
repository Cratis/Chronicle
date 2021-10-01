// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.AspNetCore.Workbench;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;

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
            applicationBuilder.PerformBootProcedures();
            applicationBuilder.RunAsSinglePageApplication(filesOptions);

            return applicationBuilder;
        }
    }
}
