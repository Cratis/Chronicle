// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_NamespaceOpeningGuard;

public class when_creating_a_new_namespace_from_a_single_threaded_scheduler : given.a_guard_on_a_single_threaded_scheduler
{
    Exception? _result;

    async Task Because() => _result = await OnTheScheduler(() => _guard.EnsureCanCreateIfNew(EventStore, Namespace, _namespaces));

    [Fact] void should_complete_without_waiting_on_its_own_scheduler() => _result.ShouldBeNull();
    [Fact] void should_check_the_creation() => _gate.Received(1).EnsureCanCreate(Arg.Is<ResourceCreation>(_ => _.Namespace == Namespace));
}
