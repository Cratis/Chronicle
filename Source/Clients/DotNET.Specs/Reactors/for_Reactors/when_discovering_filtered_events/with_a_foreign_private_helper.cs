// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_discovering_filtered_events;

public class with_a_foreign_private_helper : given.a_filtered_provider
{
    Task Because() => DiscoverAndSubscribe();

    [Fact] void should_observe_the_event_log() => _handler.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_observe_only_the_registered_handler_event() => _handler.EventTypes.ShouldEqual([_registeredIdentity]);
    [Fact] void should_not_subscribe_to_the_helpers_foreign_store() => _subscriptions.ShouldBeEmpty();
}
