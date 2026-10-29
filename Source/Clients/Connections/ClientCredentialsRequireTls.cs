// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a connection string asks for client credentials on a connection without TLS.
/// </summary>
/// <remarks>
/// Chronicle serves its token endpoint over TLS only; with <c language="csharp">tls.enabled=false</c> and authentication
/// turned on it requires an external HTTPS authority. A client connecting with <c language="csharp">disableTls=true</c>
/// therefore has nowhere to exchange client credentials for a token.
/// </remarks>
public class ClientCredentialsRequireTls()
    : Exception(
        "The connection string combines client credentials with disableTls=true, but Chronicle only serves its token endpoint (/connect/token) over TLS. " +
        "Use auth=none against a server with authentication turned off, apiKey=<token> with a token from the server's external authority, or connect with TLS.");
