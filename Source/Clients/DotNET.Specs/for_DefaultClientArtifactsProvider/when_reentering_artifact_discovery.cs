// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_reentering_artifact_discovery : given.an_artifacts_provider
{
    Exception _error;
    IEnumerable<Type> _reentrantArtifacts;
    IEnumerable<Type> _eventTypes;
    int _definitionReads;

    void Establish() => _assembliesProvider.DefinedTypes.Returns(_ =>
    {
        // The event types are already populated, but the remaining artifacts are not ready.
        if (++_definitionReads == 2) _reentrantArtifacts = _provider.EventTypes;
        return _definedTypes;
    });

    void Because()
    {
        _error = Catch.Exception(() => _ = _provider.EventTypes);
        _eventTypes = _provider.EventTypes;
    }

    [Fact] void should_reject_reentrant_access() => _error.ShouldNotBeNull();
    [Fact] void should_not_expose_already_populated_artifacts() => _reentrantArtifacts.ShouldBeNull();
    [Fact] void should_retry_initialization() => _assembliesProvider.Received(2).Initialize();
    [Fact] void should_discover_artifacts_on_retry() => _eventTypes.ShouldContainOnly(typeof(ClientArtifactDiscovered));
}
