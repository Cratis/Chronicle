// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_the_namespace_requires_recovery : given.an_incarnation_enabled_provider
{
    Exception? _error;

    void Establish() => lifecycle = lifecycle! with { Status = NamespaceLifecycleStatus.RecoveryRequired };

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => provider.ReadStateAsync("name", grainId, state));

    [Fact] void should_surface_the_recovery_required_state() => _error.ShouldBeOfExactType<NamespaceRecoveryRequired>();
    [Fact] void should_not_open_namespace_storage() => eventStoreStorage.DidNotReceive().GetNamespace(Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_not_hydrate_state() => eventSequenceStorage.DidNotReceive().GetState();
}
