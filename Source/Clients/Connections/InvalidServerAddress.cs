// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a server address in a connection string cannot be parsed.
/// </summary>
/// <param name="address">The server address that could not be parsed. Only its port part is echoed in the message.</param>
public class InvalidServerAddress(string address) : Exception($"The server address '{Mask(address)}' is not a valid host with an optional port")
{
    /// <summary>
    /// Masks everything before the last colon, as an address in a malformed connection string may hold user information.
    /// </summary>
    /// <param name="address">The address to mask.</param>
    /// <returns>The masked address.</returns>
    static string Mask(string address)
    {
        var colonIndex = address.LastIndexOf(':');
        return colonIndex == -1 ? "REDACTED" : $"REDACTED{address[colonIndex..]}";
    }
}
