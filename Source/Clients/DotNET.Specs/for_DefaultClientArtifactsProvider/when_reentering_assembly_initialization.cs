// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_reentering_assembly_initialization : given.an_artifacts_provider
{
    Exception _error;
    IEnumerable<Type> _reentrantArtifacts;
    IEnumerable<Type> _eventTypes;
    int _attempts;

    void Establish() => _assembliesProvider.When(_ => _.Initialize()).Do(_ =>
    {
        // Reenter only once so a missing guard fails assertions instead of overflowing the stack.
        if (++_attempts == 1) _reentrantArtifacts = _provider.EventTypes;
    });

    void Because()
    {
        _error = Catch.Exception(() => _ = _provider.EventTypes);
        _eventTypes = _provider.EventTypes;
    }

    [Fact] void should_reject_reentrant_access() => _error.ShouldNotBeNull();
    [Fact] void should_explain_the_initialization_cycle() => (_error?.Message).ShouldEqual("Client artifacts cannot be accessed reentrantly during initialization. Complete assembly discovery before accessing client artifacts.");
    [Fact] void should_not_return_partial_artifacts() => _reentrantArtifacts.ShouldBeNull();
    [Fact] void should_allow_a_later_retry() => _attempts.ShouldEqual(2);
    [Fact] void should_discover_artifacts_on_retry() => _eventTypes.ShouldContainOnly(typeof(ClientArtifactDiscovered));
}
