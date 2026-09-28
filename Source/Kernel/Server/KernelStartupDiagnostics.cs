// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

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
}
