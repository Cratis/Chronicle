// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Federation;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents an implementation of <see cref="IFederationCaller"/> that reads the client certificate from the current HTTP connection.
/// </summary>
/// <remarks>
/// The certificate is only ever taken from a connection accepted by the federation listener. A certificate on any other
/// listener, or none, is reported as no certificate, so it can never authenticate a federation pull.
/// </remarks>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> giving the current request.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/>.</param>
internal sealed class HttpFederationCaller(IHttpContextAccessor httpContextAccessor, IOptions<ChronicleOptions> options) : IFederationCaller
{
    /// <inheritdoc/>
    public X509Certificate2? ClientCertificate
    {
        get
        {
            var connection = httpContextAccessor.HttpContext?.Connection;
            return connection is not null && FederationPortPolicy.IsFederationPort(options.Value, connection.LocalPort)
                ? connection.ClientCertificate
                : null;
        }
    }
}
