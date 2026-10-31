// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Reports identities already durable when an atomic batch could not be written. No event from the attempted batch was written.
/// </summary>
/// <param name="Receipts">The durable receipts keyed by identity. Absent identities are omitted.</param>
public record IdentitiesAlreadyDurable(IReadOnlyDictionary<string, StoredEventPublication> Receipts);
