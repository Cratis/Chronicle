// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKeys.given;

public class a_key_store : Specification
{
    protected static readonly byte[] _canonical = [1, 2, 3];
    protected InMemoryEncryptionKeyStorage _keyStore;

    void Establish() => _keyStore = new();

    protected static EventSequenceMutationDigestKeys KeysOver(IEncryptionKeyStorage keyStore) =>
        new(keyStore, NullLogger<EventSequenceMutationDigestKeys>.Instance);
}
