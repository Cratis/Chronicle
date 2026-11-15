// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Gives the federation service the client certificate the peer presented on the current connection.
/// Implemented in the server on top of the HTTP connection; never derived from request content.
/// </summary>
public interface IFederationCaller
{
    /// <summary>
    /// Gets the presented client certificate, or null when there is none.
    /// </summary>
    X509Certificate2? ClientCertificate { get; }
}
