// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines how browser and HTTP API callers - the Workbench and its API - are authenticated.
/// gRPC clients are never affected; they always authenticate with their client credentials.
/// </summary>
public enum HttpAuthenticationMode
{
    /// <summary>
    /// Chronicle authenticates HTTP callers itself, with Workbench username and password sign-in. This is the default.
    /// </summary>
    Chronicle = 0,

    /// <summary>
    /// A reverse proxy in front of Chronicle authenticates HTTP callers and forwards the signed-in user in request headers.
    /// Chronicle accepts those headers only from a trusted proxy, and never on gRPC calls.
    /// </summary>
    TrustedProxy = 1
}
