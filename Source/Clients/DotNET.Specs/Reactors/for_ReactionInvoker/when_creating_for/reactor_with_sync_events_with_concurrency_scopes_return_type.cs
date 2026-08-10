// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class reactor_with_sync_events_with_concurrency_scopes_return_type : given.an_reactor_invoker_for<ReactorWithSyncEventsWithConcurrencyScopesReturnType>
{
    [Fact] void should_recognize_the_handler_method() => _reactorEventTypes.ShouldContainOnly(typeof(MyEvent).GetEventType());
}
