// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Protects kernel event provenance with the deployment's Data Protection key ring.
/// </summary>
/// <param name="provider">The deployment's Data Protection provider.</param>
[Singleton]
public class KernelEventProof(IDataProtectionProvider provider) : IKernelEventProof
{
    readonly IDataProtector _protector = provider.CreateProtector("Cratis.Chronicle.KernelEventProof.v1");

    /// <inheritdoc/>
    public string Sign(EventType type, EventSourceId source, EventSequenceNumber predecessor, string body) =>
        _protector.Protect(Payload(type, source, predecessor, body));

    /// <inheritdoc/>
    public bool Verify(EventType type, EventSourceId source, EventSequenceNumber predecessor, string body, string? proof)
    {
        if (string.IsNullOrWhiteSpace(proof)) return false;
        try
        {
            var actual = Encoding.UTF8.GetBytes(_protector.Unprotect(proof));
            var expected = Encoding.UTF8.GetBytes(Payload(type, source, predecessor, body));
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    static string Payload(EventType type, EventSourceId source, EventSequenceNumber predecessor, string body) =>
        JsonSerializer.Serialize(new[] { type.Id.Value, source.Value, predecessor.Value.ToString(CultureInfo.InvariantCulture), body });
}
