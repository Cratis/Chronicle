// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

internal static partial class KernelConfigurationLogging
{
    [LoggerMessage(LogLevel.Warning, "identityProvider.certificate is configured but ignored. With the internal authority, /connect/token uses the top-level tls certificate. With authentication enabled, tls.enabled=false requires an external HTTPS authority; Chronicle does not serve /connect/token in that mode. Remove identityProvider.certificate")]
    internal static partial void IdentityProviderCertificateIgnored(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Warning, "TLS is disabled on the Chronicle port: the backend serves cleartext HTTP/2 only. A TLS-terminating reverse proxy forwarding all traffic over h2c is expected; do not expose this port directly")]
    internal static partial void TlsDisabledBehindProxy(this ILogger<Kernel> logger);
}
