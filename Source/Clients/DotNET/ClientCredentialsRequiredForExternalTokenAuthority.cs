// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// The exception that is thrown when an external token authority is configured but the connection string carries no client credentials.
/// </summary>
/// <remarks>
/// The development credentials are never sent to an external authority.
/// </remarks>
public class ClientCredentialsRequiredForExternalTokenAuthority() :
    Exception("An external token authority (Authentication:TokenEndpoint or Authentication:Authority) requires client credentials (username and password) in the connection string.");
