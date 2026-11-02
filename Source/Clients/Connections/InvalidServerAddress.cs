// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a server address in a connection string cannot be parsed.
/// </summary>
/// <param name="address">The server address that could not be parsed. Only a numeric port part is echoed in the message.</param>
public class InvalidServerAddress(string address) : Exception($"The server address '{Mask(address)}' is not a valid host with an optional port")
{
    /// <summary>
    /// Masks the address, as an address in a malformed connection string may hold user information.
    /// Only a numeric port after the last colon is kept.
    /// </summary>
    /// <param name="address">The address to mask.</param>
    /// <returns>The masked address.</returns>
    static string Mask(string address)
    {
        var colonIndex = address.LastIndexOf(':');
        var hasNumericPort = colonIndex != -1 && colonIndex < address.Length - 1 && address[(colonIndex + 1)..].All(char.IsAsciiDigit);
        return hasNumericPort ? $"REDACTED{address[colonIndex..]}" : "REDACTED";
    }
}
