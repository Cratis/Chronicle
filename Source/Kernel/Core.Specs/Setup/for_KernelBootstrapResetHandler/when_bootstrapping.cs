// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Setup.for_KernelBootstrapResetHandler;

/// <summary>
/// The reset drops the System event store's database along with the event type schemas registered into it.
/// Nothing else re-registers them — every other store gets them back through <c>EventStores.Ensure</c> when a
/// client reconnects — so bootstrap has to, or the kernel's own events cannot be appended afterwards.
/// </summary>
public class when_bootstrapping : given.a_bootstrap_reset_handler
{
    Task Because() => Bootstrap();

    [Fact] void should_ensure_the_default_system_namespace() => _systemNamespaces.Received(1).EnsureDefault();
    [Fact] void should_register_the_system_event_store_event_types() => _eventTypes.Received(1).DiscoverAndRegister(EventStoreName.System);
    [Fact] void should_register_the_kernel_reactors() => _reactors.Received(1).DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default);
    [Fact] void should_ensure_the_default_admin_user() => EnsureDefaultAdminUserCount.ShouldEqual(1);
    [Fact] void should_ensure_the_bootstrap_clients() => EnsureBootstrapClientsCount.ShouldEqual(1);
}
