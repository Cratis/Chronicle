// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for using Cratis with a <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Use Cratis workbench.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
        /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
        public static IServiceCollection AddCratisWorkbench(this IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(_ => _.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
            return services;
        }
    }
}
