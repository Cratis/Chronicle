// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_persisted_metadata_differs : given.registrations
{
    async Task Establish()
    {
        await _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
        var definition = _persisted.Values.Single();
        _persisted[definition.Identifier] = definition with { IsReplayable = true };
        _definitions.ClearReceivedCalls();
    }

    Task Because() => _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);

    [Fact] async Task should_persist_the_changed_metadata() => await _definitions.Received(1).Save(Arg.Any<ReactorDefinition>());
    [Fact] void should_restore_the_declared_replay_policy() => _persisted.Values.Single().IsReplayable.ShouldBeFalse();
}
