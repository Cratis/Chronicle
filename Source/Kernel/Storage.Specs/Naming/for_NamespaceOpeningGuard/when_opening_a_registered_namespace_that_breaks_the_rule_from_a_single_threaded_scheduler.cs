// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Naming.for_NamespaceOpeningGuard;

public class when_opening_a_registered_namespace_that_breaks_the_rule_from_a_single_threaded_scheduler : given.a_guard_on_a_single_threaded_scheduler
{
    Exception? _result;

    void Establish()
    {
        _gate.When(_ => _.EnsureCanOpen(EventStore, Namespace)).Throw(new InvalidResourceName("breaks the rule", default));
        _namespaces.GetAll().Returns(_ => CompletingOnTheCaller<IEnumerable<NamespaceState>>([new(Namespace, DateTimeOffset.UnixEpoch)]));
    }

    async Task Because() => _result = await OnTheScheduler(() => _guard.EnsureCanOpen(EventStore, Namespace, _namespaces));

    [Fact] void should_complete_without_waiting_on_its_own_scheduler() => _result.ShouldBeNull();
    [Fact] void should_mark_it_registered() => _gate.Received(1).MarkRegistered(EventStore, Namespace);
}
