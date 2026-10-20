// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Classifies addresses that must not be treated as public webhook receivers.
/// </summary>
internal static class WebhookAddressRules
{
    /// <summary>
    /// Determines whether an address is a cloud metadata service.
    /// </summary>
    /// <param name="address">The resolved address.</param>
    /// <returns>Whether explicit approval is always required.</returns>
    internal static bool IsMetadata(IPAddress address)
    {
        address = Normalize(address);
        return address.Equals(IPAddress.Parse("169.254.169.254")) || address.Equals(IPAddress.Parse("fd00:ec2::254"));
    }

    /// <summary>
    /// Determines whether an address is private, reserved, or special-purpose.
    /// </summary>
    /// <param name="address">The resolved address.</param>
    /// <returns>Whether the address is not public.</returns>
    internal static bool IsSpecial(IPAddress address)
    {
        address = Normalize(address);
        if (IPAddress.IsLoopback(address)) return true;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] is 0 or 10 or 127 or >= 224 ||
                (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
                (bytes[0] == 169 && bytes[1] == 254) ||
                (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                (bytes[0] == 192 && (bytes[1] == 168 || (bytes[1] == 0 && bytes[2] is 0 or 2))) ||
                (bytes[0] == 198 && (bytes[1] is 18 or 19 || (bytes[1] == 51 && bytes[2] == 100))) ||
                (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
        }

        // Only global unicast is a public destination. Exclude documentation and transition ranges too.
        return (bytes[0] & 0xe0) != 0x20 ||
            (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8) ||
            (bytes[0] == 0x20 && bytes[1] == 0x02);
    }

    static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
