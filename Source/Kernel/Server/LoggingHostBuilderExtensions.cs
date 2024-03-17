// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Serilog;
using Serilog.Exceptions;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring logging for a host.
/// </summary>
public static class LoggingHostBuilderExtensions
{
    /// <summary>
    /// Use default logging.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to use with.</param>
    /// <returns><see cref="ILoggerFactory"/> for continuation.</returns>
    public static IHostBuilder UseLogging(this IHostBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithExceptionDetails()
            .ReadFrom.Configuration(ConfigurationHostBuilderExtensions.Configuration)
            .CreateLogger();

        builder.UseSerilog();

        Serilog.Debugging.SelfLog.Enable(Console.WriteLine);

        return builder;
    }
}
