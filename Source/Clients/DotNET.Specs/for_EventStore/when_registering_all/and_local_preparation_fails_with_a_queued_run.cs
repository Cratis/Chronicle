// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_local_preparation_fails_with_a_queued_run : given.an_event_store_with_a_projection_that_cannot_be_built
{
    TaskCompletionSource _release;
    RegistrationOutcome _during;
    readonly Exception _failure = new("local preparation failed");
    Action<Exception> _publish;

    void Establish()
    {
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SetField("_registerAllFlight", new SingleFlightRegistration(() => _release.Task));
        _publish = typeof(EventStore).GetMethod("PublishLocalPreparationFailure", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Action<Exception>>(_eventStore);
    }

    async Task Because()
    {
        var run = _eventStore.RegisterAll();
        _publish(_failure);
        _during = _eventStore.Registration;
        _release.SetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_remain_registering_until_the_queued_run_finishes() => _during.IsRegistering.ShouldBeTrue();
    [Fact] void should_publish_the_local_failure() => _during.Failure.ShouldBeSame(_failure);
    [Fact] void should_clear_registering_after_the_task_finishes() => _eventStore.Registration.IsRegistering.ShouldBeFalse();
}
