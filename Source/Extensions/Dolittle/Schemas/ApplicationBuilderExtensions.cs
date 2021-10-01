// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Dolittle.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Represents extension methods for building on <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Add the Dolittle schema store extension.
        /// /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/> to add to.</param>
        /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
        public static IApplicationBuilder UseDolittleSchemaStore(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder
                .ApplicationServices
                .GetService<ISchemaStore>()?
                .DiscoverGenerateAndConsolidate();
            return applicationBuilder;
        }
    }
}
