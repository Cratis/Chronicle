// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StorePreparationGate;

public class when_preparation_is_pending : Specification
{
    StorePreparationGate _gate;
    TaskCompletionSource _prepared;
    Task _discovery;
    Task _joined;
    Task _registration;
    bool _registeredBeforePreparation;
    int _preparations;
    int _registrations;

    void Establish()
    {
        _gate = new();
        _prepared = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _discovery = _gate.Discover(() =>
        {
            _preparations++;
            return _prepared.Task;
        });
    }

    async Task Because()
    {
        _joined = _gate.Discover(() => throw new Exception("A pending revision must be joined."));
        _registration = _gate.Register(() => throw new Exception("Registration must not start a second preparation."), () =>
        {
            _registrations++;
            return Task.CompletedTask;
        });
        _registeredBeforePreparation = _registrations != 0;
        _prepared.SetResult();
        await Task.WhenAll(_registration, _discovery, _joined).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_join_the_published_verdict() => ReferenceEquals(_discovery, _joined).ShouldBeTrue();
    [Fact] void should_prepare_once() => _preparations.ShouldEqual(1);
    [Fact] void should_not_register_while_preparation_is_pending() => _registeredBeforePreparation.ShouldBeFalse();
    [Fact] void should_register_after_the_successful_verdict() => _registrations.ShouldEqual(1);
}
