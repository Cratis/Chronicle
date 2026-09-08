// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_initializing_artifacts : given.an_artifacts_provider
{
    IEnumerable<Type>[] _artifacts;
    IEnumerable<Type>[] _cachedArtifacts;
    int _definitionReads;
    int _initialDefinitionReads;

    void Establish() => _assembliesProvider.DefinedTypes.Returns(_ =>
    {
        _definitionReads++;
        return _definedTypes;
    });

    void Because()
    {
        _artifacts = ReadArtifacts();
        _initialDefinitionReads = _definitionReads;
        _definedTypes = [];
        _cachedArtifacts = ReadArtifacts();
    }

    [Fact] void should_discover_event_types() => _provider.EventTypes.ShouldContainOnly(typeof(ClientArtifactDiscovered));
    [Fact] void should_populate_property_constraints() => _provider.UniqueConstraints.ShouldContainOnly(typeof(ClientArtifactDiscovered));
    [Fact] void should_populate_event_type_constraints() => _provider.UniqueEventTypeConstraints.ShouldContainOnly(typeof(ClientArtifactDiscovered));
    [Fact] void should_populate_removed_constraints() => _provider.RemoveConstraintEventTypes.ShouldContainOnly(typeof(ClientArtifactDiscovered));
    [Fact] void should_populate_the_last_artifact_collection() => _provider.EventTypeMigrators.ShouldContainOnly(typeof(EventTypeMigration<,>));
    [Fact] void should_cache_every_artifact_collection() => _artifacts.Zip(_cachedArtifacts).All(_ => ReferenceEquals(_.First, _.Second)).ShouldBeTrue();
    [Fact] void should_initialize_assemblies_once() => _assembliesProvider.Received(1).Initialize();
    [Fact] void should_not_repeat_type_discovery() => _definitionReads.ShouldEqual(_initialDefinitionReads);
}
