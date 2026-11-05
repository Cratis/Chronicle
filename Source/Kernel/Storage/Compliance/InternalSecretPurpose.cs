// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents what a kernel-internal secret is for.
/// </summary>
/// <remarks>
/// <para>
/// Internal secrets live beside subject encryption keys but never among them: a key store holds them in a location
/// that no <see cref="Chronicle.Compliance.EncryptionKeyIdentifier"/> can address, so nothing reachable from the
/// compliance surface can read, erase or re-provision one.
/// </para>
/// <para>
/// The set of purposes is closed - the constructor is private - and every value consists only of lowercase ASCII
/// letters, digits and single hyphens. That keeps each purpose valid unchanged as a document id, a table key and a
/// secret-name segment in every backend.
/// </para>
/// </remarks>
public record InternalSecretPurpose : ConceptAs<string>
{
    /// <summary>
    /// The per-event-store secret mutation receipt digests are keyed from.
    /// </summary>
    public static readonly InternalSecretPurpose MutationReceiptDigest = new("mutation-receipt-digest-v2");

    InternalSecretPurpose(string value)
        : base(value)
    {
    }
}
