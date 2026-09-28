// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

internal static partial class KernelConfigurationLogging
{
    [LoggerMessage(LogLevel.Warning, "identityProvider.certificate is configured but ignored. The top-level tls certificate serves /connect/token and every other endpoint when TLS is enabled; if tls.enabled=false, the HTTPS proxy's certificate serves them instead. Remove identityProvider.certificate")]
    internal static partial void IdentityProviderCertificateIgnored(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Warning, "TLS is disabled on the Chronicle port: the backend serves cleartext HTTP/2 only. A TLS-terminating reverse proxy forwarding all traffic over h2c is expected; do not expose this port directly")]
    internal static partial void TlsDisabledBehindProxy(this ILogger<Kernel> logger);
}
