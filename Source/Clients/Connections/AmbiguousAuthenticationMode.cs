// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when both client credentials and API key authentication are specified in a connection string.
/// </summary>
public class AmbiguousAuthenticationMode() :
    Exception("Cannot specify both client credentials (username/password) and API key authentication in the same connection string. Please use only one authentication method.");
