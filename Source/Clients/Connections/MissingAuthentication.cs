// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception thrown when there is no clear authentication method specified.
/// </summary>
public class MissingAuthentication()
    : Exception("No authentication method specified in the connection string. Please provide either client credentials or an API key. Chronicle connections require authentication.");
