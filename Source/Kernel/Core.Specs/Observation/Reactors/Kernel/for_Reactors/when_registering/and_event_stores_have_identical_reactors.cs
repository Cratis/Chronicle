// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_event_stores_have_identical_reactors : given.registrations
{
    Task Because() => Task.WhenAll(
        _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default),
        _reactors.DiscoverAndRegister(_otherEventStore, EventStoreNamespaceName.Default));

    [Fact] async Task should_persist_the_first_stores_definition() => await _definitions.Received(1).Save(Arg.Any<ReactorDefinition>());
    [Fact] async Task should_persist_the_second_stores_definition() => await _otherDefinitions.Received(1).Save(Arg.Any<ReactorDefinition>());
}
