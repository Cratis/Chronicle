// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_the_namespace_is_not_open : given.an_incarnation_enabled_provider
{
    readonly List<Exception?> _errors = [];

    async Task Because()
    {
        foreach (var status in new[] { NamespaceLifecycleStatus.Preparing, NamespaceLifecycleStatus.Closing, NamespaceLifecycleStatus.Removed })
        {
            lifecycle = lifecycle! with { Status = status };
            _errors.Add(await Cratis.Specifications.Catch.Exception(() => provider.ReadStateAsync("name", grainId, state)));
        }
    }

    [Fact] void should_refuse_each_non_open_lifecycle() => _errors.ForEach(_ => _.ShouldBeOfExactType<NamespaceClosing>());
    [Fact] void should_not_open_namespace_storage() => eventStoreStorage.DidNotReceive().GetNamespace(Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_not_hydrate_state() => eventSequenceStorage.DidNotReceive().GetState();
    [Fact] void should_not_load_the_tail() => eventSequenceStorage.DidNotReceive().GetTailSequenceNumber();
}
