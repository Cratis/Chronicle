// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;

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
        public static ILoggerFactory UseDefaultLogging(this IHostBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .ReadFrom.Configuration(ConfigurationHostBuilderExtensions.Configuration)
                .CreateLogger();

            builder.UseSerilog();

            Serilog.Debugging.SelfLog.Enable(Console.WriteLine);

            return new Serilog.Extensions.Logging.SerilogLoggerFactory();
        }
    }
}
