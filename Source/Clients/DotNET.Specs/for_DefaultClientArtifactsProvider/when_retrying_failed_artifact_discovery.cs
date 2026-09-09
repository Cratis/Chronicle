// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_retrying_failed_artifact_discovery : given.an_artifacts_provider
{
    Exception _failure;
    Exception _error;
    IEnumerable<Type> _eventTypes;
    int _definitionReads;

    void Establish()
    {
        _failure = new Exception("Artifact discovery failed after discovering event types.");
        _assembliesProvider.DefinedTypes.Returns(_ =>
        {
            if (++_definitionReads == 2) throw _failure;
            return _definedTypes;
        });
    }

    void Because()
    {
        _error = Catch.Exception(() => _ = _provider.EventTypes);
        _definedTypes = [typeof(ReplacementClientArtifactDiscovered).GetTypeInfo(), typeof(EventTypeMigration<,>).GetTypeInfo()];
        _eventTypes = _provider.EventTypes;
    }

    [Fact] void should_propagate_the_original_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_retry_assembly_initialization() => _assembliesProvider.Received(2).Initialize();
    [Fact] void should_replace_the_unpublished_event_types() => _eventTypes.ShouldContainOnly(typeof(ReplacementClientArtifactDiscovered));
    [Fact] void should_recompute_dependent_constraints() => _provider.UniqueConstraints.ShouldBeEmpty();
    [Fact] void should_complete_the_last_discovery_step() => _provider.EventTypeMigrators.ShouldContainOnly(typeof(EventTypeMigration<,>));
}
