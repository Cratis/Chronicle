// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The routes of the enrollment surface, served on the federation listener.
/// </summary>
public static class FederationEnrollmentRoutes
{
    /// <summary>
    /// The route that returns the authority certificate, which is public. A peer checks it against the fingerprint in its enrollment code.
    /// </summary>
    public const string Authority = "/federation/enrollment/authority";

    /// <summary>
    /// The route that redeems an enrollment. It is the only route on the federation listener that does not require a client certificate, besides <see cref="Authority"/>.
    /// </summary>
    public const string Redeem = "/federation/enrollment/redeem";

    /// <summary>
    /// The route that renews a certificate. It requires the certificate to renew, presented over mutual TLS.
    /// </summary>
    public const string Renew = "/federation/enrollment/renew";

    /// <summary>
    /// The largest request body the enrollment routes accept, in bytes.
    /// </summary>
    public const long MaxRequestBodySize = 8192;
}
