// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.when_ensuring;

public class and_the_marker_is_already_written : given.an_event_store_initialization
{
    void Establish() => _initialized = true;
    Task Because() => _initialization.Ensure(_name);

    [Fact] void should_read_the_marker_once() => _store.Received(1).IsInitialized();
    [Fact] void should_not_call_the_initializer() => _initializerReference.DidNotReceive().Ensure();
}
