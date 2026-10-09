// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Identifies one immutable, durable publication intent independently of its allocated sequence slot.
/// </summary>
/// <param name="Id">An opaque deterministic identity, including observer, replay occurrence, input progress,
/// target key, source sequence, destination sequence, store and namespace. Encode components unambiguously.</param>
/// <param name="Fingerprint">A digest of the immutable intent, including payload and all append context.
/// Generate before protection (encryption may be nondeterministic), and persist with the intent.</param>
public record EventPublication(string Id, string Fingerprint);
