// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a single network endpoint the Chronicle server binds.
/// </summary>
/// <param name="Port">The TCP port to listen on.</param>
/// <param name="Protocols">The <see cref="EndpointProtocols"/> served on the endpoint.</param>
/// <param name="UseTls">Whether the endpoint terminates TLS and therefore requires a certificate.</param>
public record ServerEndpoint(int Port, EndpointProtocols Protocols, bool UseTls);
