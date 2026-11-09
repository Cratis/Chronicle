// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_registration_faults_after_the_deadline : given.a_registration_lifetime
{
    readonly InvalidOperationException _failure = new("Registration failed after its deadline");
    TaskCompletionSource? _lateRegistration;
    TaskStatus _lateRegistrationStatus;
    int _unobserved;
    bool _terminatedBeforeFault;

    void Establish()
    {
        // Inline continuations make SetException return only after the production fault observer has run.
        _lateRegistration = new TaskCompletionSource();
        _register = () => _lateRegistration.Task;
        TaskScheduler.UnobservedTaskException += OnUnobserved;
    }

    async Task Because()
    {
        Connect();
        await Arrives(_entered.Task);
        await ExpireRegistration();
        _terminatedBeforeFault = _terminal.Task.IsCompleted;
        _lateRegistrationStatus = FaultAndReleaseRegistration();
        _clock.ReleaseTimer();

        // Best effort only: should the released task be finalized here, an unobserved fault would be reported.
        // No fact depends on whether the collector actually reclaims it.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    void Destroy()
    {
        TaskScheduler.UnobservedTaskException -= OnUnobserved;
        _lateRegistration?.TrySetResult();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    TaskStatus FaultAndReleaseRegistration()
    {
        var registration = _lateRegistration!;
        registration.SetException(_failure);
        _lateRegistration = null;
        _register = () => Task.CompletedTask;

        // Status only: reading the exception here would observe the fault on behalf of the production code.
        return registration.Task.Status;
    }

    void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        if (args.Exception.InnerExceptions.Any(_ => ReferenceEquals(_, _failure)))
        {
            Interlocked.Increment(ref _unobserved);
        }
    }

    [Fact] void should_have_terminated_before_the_late_fault() => _terminatedBeforeFault.ShouldBeTrue();
    [Fact] void should_fault_the_abandoned_registration() => _lateRegistrationStatus.ShouldEqual(TaskStatus.Faulted);
    [Fact] void should_observe_the_late_fault() => Volatile.Read(ref _unobserved).ShouldEqual(0);
    [Fact] void should_not_send_another_terminal_notification() => _errors.Count.ShouldEqual(1);
    [Fact] void should_not_activate_the_connection() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_open_locally() => LocallyOpen.ShouldBeFalse();
}
