// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a server address in a connection string cannot be parsed.
/// </summary>
/// <param name="address">The server address that could not be parsed.</param>
public class InvalidServerAddress(string address) : Exception($"The server address '{address}' is not a valid host with an optional port");
