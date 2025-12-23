// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

internal static partial class KernelLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Generated development certificate and CA for local development")]
    internal static partial void GeneratedDevelopmentCertificate(this ILogger<Kernel> logger);

    [LoggerMessage(2, LogLevel.Warning, "Failed generating development certificate for local development")]
    internal static partial void FailedGeneratingDevelopmentCertificate(this ILogger<Kernel> logger, Exception ex);

    [LoggerMessage(3, LogLevel.Information, "Serving development CA on port {Port}")]
    internal static partial void ServingDevelopmentCa(this ILogger<Kernel> logger, int port);

    [LoggerMessage(4, LogLevel.Error, "Failed serving development CA endpoint")]
    internal static partial void FailedServingDevelopmentCa(this ILogger<Kernel> logger, Exception ex);
}
