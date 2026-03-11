// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Reactors.Kernel;

namespace Cratis.Chronicle.Setup.for_ChronicleInitializer.when_initializing;

public class and_there_are_no_event_stores : given.a_chronicle_initializer
{
    async Task Because() => await _initializer.Initialize();

    [Fact] void should_ensure_default_system_namespace() =>
        _systemNamespacesGrain.Received(1).EnsureDefault();
    [Fact] void should_discover_and_register_system_reactors() =>
        _reactors.Received(1).DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default);
    [Fact] void should_ensure_default_admin_user() =>
        _authenticationService.Received(1).EnsureDefaultAdminUser();
    [Fact] void should_not_discover_event_types_for_any_event_store() =>
        _eventTypes.DidNotReceiveWithAnyArgs().DiscoverAndRegister(default!);
}
