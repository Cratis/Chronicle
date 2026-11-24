// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationQueue;

public class when_a_registration_fails : Specification
{
    readonly TaskCompletionSource _fail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    RegistrationQueue _queue;
    Exception _first;
    Exception _joined;
    int _applications;
    bool _followUpApplied;

    void Establish() => _queue = new(2, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5), new ManualTimeProvider());

    async Task Because()
    {
        Task Submit() => _queue.Run("registration", async _ =>
        {
            _applications++;
            await _fail.Task;
        });
        var first = Submit();
        var joined = Submit();
        _fail.SetException(new InvalidOperationException("failed"));
        _first = await Catch.Exception(async () => await first);
        _joined = await Catch.Exception(async () => await joined);
        await _queue.Run("registration", _ =>
        {
            _followUpApplied = true;
            return Task.CompletedTask;
        });
    }

    [Fact] void should_fail_the_first_caller() => _first.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_the_caller_that_joined() => _joined.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_have_applied_it_once() => _applications.ShouldEqual(1);
    [Fact] void should_let_a_later_registration_with_the_same_key_lead_its_own() => _followUpApplied.ShouldBeTrue();
}
