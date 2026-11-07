// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1819 // Allow arrays for properties

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Represents the stored version of a kernel-internal secret for an <see cref="InternalSecretPurpose"/>.
/// </summary>
/// <remarks>
/// Internal secrets live in their own collection in the event store database. Subject keys live in a differently named
/// collection in each namespace database, and no subject key operation reads or writes this collection.
/// </remarks>
/// <param name="Id">The exact event store and <see cref="InternalSecretPurpose"/> the secret is for.</param>
/// <param name="PublicKey">The public part of the secret.</param>
/// <param name="PrivateKey">The private part of the secret.</param>
public record InternalSecretForPurpose(string Id, byte[] PublicKey, byte[] PrivateKey);
