// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_a_write_fails : given.registrations
{
    Exception? _failure;
    int _attempts;

    void Establish() => _definitions.Save(Arg.Any<ReactorDefinition>()).Returns(call =>
    {
        if (++_attempts == 1)
        {
            return Task.FromException(new TimeoutException("Planted persistence failure"));
        }

        var definition = call.Arg<ReactorDefinition>();
        _persisted[definition.Identifier] = definition;
        return Task.CompletedTask;
    });

    async Task Because()
    {
        _failure = await Catch.Exception(() => _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default));
        await _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
    }

    [Fact] void should_fail_the_first_registration() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] async Task should_release_admission_and_retry_persistence() => await _definitions.Received(2).Save(Arg.Any<ReactorDefinition>());
    [Fact] void should_subscribe_only_after_success() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.Subscribe)).ShouldEqual(1);
}
