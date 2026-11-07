// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_computing_the_definitions_fingerprint;

public class and_the_gate_already_has_a_failed_revision : when_preparing_for_registration.given.an_explicit_store
{
    Exception _preparationFailure;
    Exception _diagnosticFailure;
    Exception _registrationFailure;
    bool _unchanged;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("failed"));
        _store = CreateStore();
        _generator.BeforeGenerate = (_, _, _) => throw new InvalidOperationException("existing failure");
    }

    async Task Because()
    {
        _preparationFailure = await Catch.Exception(_store.DiscoverAll);
        var before = _store.Registration;
        _diagnosticFailure = await Catch.Exception(_store.ComputeDefinitionsFingerprint);
        _unchanged = ReferenceEquals(before, _store.Registration);
        _registrationFailure = await Catch.Exception(_store.RegisterAll);
    }

    [Fact] void should_preserve_the_failed_verdict() => _diagnosticFailure.ShouldBeSame(_preparationFailure);
    [Fact] void should_keep_the_existing_registration_failure() => _registrationFailure.ShouldBeSame(_preparationFailure);
    [Fact] void should_not_republish_the_failure() => _unchanged.ShouldBeTrue();
}
