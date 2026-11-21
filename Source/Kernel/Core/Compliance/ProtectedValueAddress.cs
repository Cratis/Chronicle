// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// The key-storage address a protected value was written under and is released from.
/// </summary>
/// <remarks>
/// A v3 envelope signs a commitment to this address, so a genuine value copied to another subject or event store fails verification.
/// The commitment is not confidential: anyone holding a value can test a guessed address against it offline.
/// </remarks>
/// <param name="EventStore">The event store the key is stored under.</param>
/// <param name="Namespace">The namespace the key is stored under.</param>
/// <param name="Identifier">The key identifier.</param>
public record ProtectedValueAddress(EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier);
