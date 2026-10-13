// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StorePreparationGate;

public class when_preparation_fails : Specification
{
    StorePreparationGate _gate;
    Exception _failure;
    Exception _first;
    Exception _reconnect;
    int _registrations;
    int _preparations;

    void Establish()
    {
        _gate = new();
        _failure = new Exception("schema failure");
    }

    async Task Because()
    {
        _first = await Catch.Exception(() => _gate.Register(Prepare, Register));
        _reconnect = await Catch.Exception(() => _gate.Register(() => throw new Exception("A reconnect must reuse the failed verdict."), Register));
    }

    [Fact] void should_preserve_the_exact_failure() => ReferenceEquals(_first, _failure).ShouldBeTrue();
    [Fact] void should_reuse_the_failed_verdict_on_reconnect() => ReferenceEquals(_reconnect, _failure).ShouldBeTrue();
    [Fact] void should_not_repeat_local_preparation() => _preparations.ShouldEqual(1);
    [Fact] void should_send_no_registration_effects() => _registrations.ShouldEqual(0);

    Task Prepare()
    {
        _preparations++;
        throw _failure;
    }

    Task Register()
    {
        _registrations++;
        return Task.CompletedTask;
    }
}
