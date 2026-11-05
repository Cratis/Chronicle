// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_NamespaceOpeningGuard;

public class when_the_creation_check_does_not_complete : given.a_guard_on_a_single_threaded_scheduler
{
    readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _result;
    Exception? _again;

    void Establish()
    {
        _gate.EnsureCanCreate(Arg.Any<ResourceCreation>()).Returns(_never.Task);
        _guard = new(_gate, () => Task.FromResult<IEnumerable<string>>([EventStore.Value]), TimeSpan.FromMilliseconds(50));
    }

    async Task Because()
    {
        _result = await OnTheScheduler(() => _guard.EnsureCanCreateIfNew(EventStore, Namespace, _namespaces));
        _again = await OnTheScheduler(() => _guard.EnsureCanCreateIfNew(EventStore, Namespace, _namespaces));
    }

    [Fact] void should_refuse_the_open() => _result.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_check_again_on_the_next_open() => _again.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_have_asked_the_gate_both_times() => _gate.Received(2).EnsureCanCreate(Arg.Any<ResourceCreation>());
    [Fact] void should_read_the_namespaces_both_times() => _namespaces.Received(2).GetAll();
}
