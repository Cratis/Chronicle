// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

public class and_the_namespace_exists : given.an_event_sequence
{
    [Fact] async Task should_not_create_the_namespace() => await _namespaces.DidNotReceiveWithAnyArgs().EnsureCanonical(EventStoreNamespace);
    [Fact] async Task should_look_the_namespace_up() => await _namespaces.Received(1).GetCanonical(EventStoreNamespace);
}
