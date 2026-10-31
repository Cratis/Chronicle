// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_discovering_filtered_events;

public class with_a_foreign_handler_and_an_excluded_helper : given.a_filtered_provider
{
    protected override Type HandlerEvent => typeof(ForeignEvent);

    Task Because() => DiscoverAndSubscribe();

    [Fact] void should_observe_only_the_handlers_foreign_inbox() => _handler.EventSequenceId.ShouldEqual(new EventSequenceId("inbox-source"));
    [Fact] void should_observe_only_the_registered_handler_event() => _handler.EventTypes.ShouldEqual([_registeredIdentity]);
    [Fact] void should_subscribe_only_to_the_handlers_store() => _subscriptions.Count.ShouldEqual(1);
    [Fact] void should_use_the_registered_handler_identity() => _subscriptions.ShouldContain(("source", _registeredIdentity.Id));
}
