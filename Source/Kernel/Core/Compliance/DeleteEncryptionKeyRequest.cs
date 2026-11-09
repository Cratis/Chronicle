// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Services;

namespace Cratis.Chronicle.Compliance;

/// <summary>The Core-owned erasure request shared by the existing Compliance wire methods.</summary>
/// <param name="EventStore">The requested event store.</param>
/// <param name="Namespace">The requested namespace.</param>
/// <param name="Identifier">The subject key identifier.</param>
/// <param name="RequestId">Optional opaque retry identity, at most 128 characters and not whitespace-only.</param>
[WireContract]
public record DeleteEncryptionKeyRequest(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EncryptionKeyIdentifier Identifier,
    string? RequestId = null);
