// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for configuring logging for a host.
    /// </summary>
    public static class LoggingHostBuilderExtensions
    {
        /// <summary>
        /// Use default logging.
        /// </summary>
        /// <param name="builder"><see creF="IHostBuilder"/> to use with.</param>
        /// <returns><see creF="IHostBuilder"/> for continuation.</returns>
        public static IHostBuilder UseDefaultLogging(this IHostBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(ConfigurationHostBuilderExtensions.Configuration)
                .CreateLogger();

            builder.UseSerilog();

            return builder;
        }
    }
}
