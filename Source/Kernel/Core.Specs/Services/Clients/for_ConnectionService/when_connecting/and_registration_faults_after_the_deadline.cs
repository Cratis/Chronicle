// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_faults_after_the_deadline : given.a_registration_lifetime
{
    readonly InvalidOperationException _failure = new("Registration failed after its grace");
    TaskCompletionSource? _lateRegistration;
    int _unobserved;
    bool _servedBeforeFault;

    void Establish()
    {
        _lateRegistration = new TaskCompletionSource();
        _register = () => _lateRegistration.Task;
        TaskScheduler.UnobservedTaskException += OnUnobserved;
    }

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        await _clock.WaitForTimerDue(Grace);
        _clock.ExpireDue(Grace);
        await Arrives(_heartbeat.Task);
        _servedBeforeFault = _heartbeat.Task.IsCompleted;

        // Inline continuations make SetException return only after the production fault logger has run.
        _lateRegistration!.SetException(_failure);
        _lateRegistration = null;
        _register = () => Task.CompletedTask;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    void Destroy()
    {
        TaskScheduler.UnobservedTaskException -= OnUnobserved;
        _lateRegistration?.TrySetResult();
    }

    void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        if (args.Exception.InnerExceptions.Any(_ => ReferenceEquals(_, _failure)))
        {
            Interlocked.Increment(ref _unobserved);
        }
    }

    [Fact] void should_have_served_the_client_before_the_late_fault() => _servedBeforeFault.ShouldBeTrue();
    [Fact] void should_log_the_late_fault() => _logger.Entries.Any(_ => ReferenceEquals(_.Exception, _failure)).ShouldBeTrue();
    [Fact] void should_observe_the_late_fault() => Volatile.Read(ref _unobserved).ShouldEqual(0);
    [Fact] void should_not_terminate_the_connection() => _errors.ShouldBeEmpty();
    [Fact] void should_have_activated_the_connection() => _heartbeats.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_open_locally() => LocallyOpen.ShouldBeTrue();
}
