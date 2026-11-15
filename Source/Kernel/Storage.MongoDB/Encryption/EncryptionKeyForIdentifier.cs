// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1819 // Allow arrays for properties

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Represents the stored version of an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyId"/>.
/// </summary>
/// <param name="Id">The composite <see cref="EncryptionKeyId"/> that uniquely identifies this key revision.</param>
/// <param name="PublicKey">The public part of the key.</param>
/// <param name="PrivateKey">The private part of the key.</param>
/// <param name="Published">Whether this revision passed fence validation; absent on legacy rows.</param>
/// <param name="StagedLifecycle">The lifecycle observed before staging.</param>
/// <param name="WriterNonce">The unique owner of this staged revision.</param>
public record EncryptionKeyForIdentifier(
    EncryptionKeyId Id,
    byte[] PublicKey,
    byte[] PrivateKey,
    bool Published = true,
    uint? StagedLifecycle = null,
    string? WriterNonce = null);
