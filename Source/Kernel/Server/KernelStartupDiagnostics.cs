// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Reports configuration settings that are retained for compatibility but not used.
/// </summary>
internal static class KernelStartupDiagnostics
{
    /// <summary>
    /// Reports an ignored identity provider certificate if one was configured.
    /// </summary>
    /// <param name="options">The Chronicle configuration.</param>
    /// <param name="logger">The kernel logger.</param>
    internal static void ReportIdentityProviderCertificate(ChronicleOptions options, ILogger<Kernel> logger)
    {
        if (options.IdentityProvider?.Certificate is not null)
        {
            logger.IdentityProviderCertificateIgnored();
        }
    }

    /// <summary>
    /// Reports settings that only the development build honors when they are configured in another build.
    /// The values are never logged.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The kernel logger.</param>
    /// <param name="developmentBuild">Whether this is the development build.</param>
    internal static void ReportIgnoredDevelopmentOnlySettings(IConfiguration configuration, ILogger<Kernel> logger, bool developmentBuild)
    {
        if (developmentBuild)
        {
            return;
        }

        if (!string.IsNullOrEmpty(configuration[$"{ChronicleOptions.SectionPath}:Authentication:DefaultAdminPassword"]))
        {
            logger.DefaultAdminPasswordIgnored();
        }
    }

    /// <summary>
    /// Reports the experimental grain directory and its cluster restart requirement when enabled.
    /// </summary>
    /// <param name="options">The Chronicle configuration.</param>
    /// <param name="logger">The kernel logger.</param>
    internal static void ReportGrainDirectory(ChronicleOptions options, ILogger<Kernel> logger)
    {
        if (options.Clustering.DistributedGrainDirectoryEnabled)
        {
            logger.DistributedGrainDirectoryEnabled();
        }
    }

    /// <summary>
    /// Reports a subscriber timeout that the Orleans response timeout on subscriber calls bounds.
    /// </summary>
    /// <param name="options">The Chronicle configuration.</param>
    /// <param name="logger">The kernel logger.</param>
    internal static void ReportSubscriberTimeout(ChronicleOptions options, ILogger<Kernel> logger)
    {
        var subscriberTimeout = options.Observers.SubscriberTimeout;
        if (subscriberTimeout <= 0)
        {
            logger.SubscriberTimeoutIndefiniteIsBounded(ObserverSubscriberResponseTimeout.Ceiling);
        }
        else if (TimeSpan.FromSeconds(subscriberTimeout) > ObserverSubscriberResponseTimeout.Ceiling)
        {
            logger.SubscriberTimeoutAboveCeiling(subscriberTimeout, ObserverSubscriberResponseTimeout.Ceiling);
        }
    }
}
