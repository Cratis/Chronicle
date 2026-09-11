// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_retrying_after_head_reuse : ArchiveRetryConformance
{
    readonly EventSequenceMutationRegistryState _state = new();

    protected override IEventSequenceMutationRegistry RecreateRegistry() => new EventSequenceMutationRegistry("store", "namespace", _state);
}
