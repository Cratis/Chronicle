// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_NamespaceOpeningGuard;

public class when_the_creation_check_fails_from_a_single_threaded_scheduler : given.a_guard_on_a_single_threaded_scheduler
{
    Exception? _result;
    Exception? _again;

    void Establish() => _gate.EnsureCanCreate(Arg.Any<ResourceCreation>()).Returns(_ => Task.FromException(new InvalidResourceName("refused", default)));

    async Task Because()
    {
        _result = await OnTheScheduler(() => _guard.EnsureCanCreateIfNew(EventStore, Namespace, _namespaces));
        _again = await OnTheScheduler(() => _guard.EnsureCanCreateIfNew(EventStore, Namespace, _namespaces));
    }

    [Fact] void should_refuse_the_open() => _result.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_refuse_it_again_rather_than_remember_it_as_checked() => _again.ShouldBeOfExactType<InvalidResourceName>();
}
