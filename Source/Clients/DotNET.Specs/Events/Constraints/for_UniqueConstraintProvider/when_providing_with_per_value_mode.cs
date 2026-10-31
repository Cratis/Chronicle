// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintProvider;

public class when_providing_with_per_value_mode : Specification
{
    UniqueConstraintProvider _provider;
    UniqueConstraintDefinition _result;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.UniqueConstraints.Returns([typeof(VersionRegistered)]);
        artifacts.RemoveConstraintEventTypes.Returns([typeof(VersionRemoved)]);
        var eventTypes = Substitute.For<IEventTypes>();
        var added = new EventType(nameof(VersionRegistered), EventTypeGeneration.First);
        var removed = new EventType(nameof(VersionRemoved), EventTypeGeneration.First);
        eventTypes.GetEventTypeFor(typeof(VersionRegistered)).Returns(added);
        eventTypes.GetEventTypeFor(typeof(VersionRemoved)).Returns(removed);
        eventTypes.GetSchemaFor(added.Id).Returns(JsonSchema.FromType<VersionRegistered>());
        eventTypes.GetSchemaFor(removed.Id).Returns(JsonSchema.FromType<VersionRemoved>());
        _provider = new(artifacts, eventTypes, new CamelCaseNamingPolicy());
    }

    void Because() => _result = (UniqueConstraintDefinition)_provider.Provide().Single();

    [Fact] void should_preserve_the_mode() => _result.Mode.ShouldEqual(UniqueConstraintMode.PerValue);
    [Fact] void should_map_the_removal_properties() => _result.RemovalEventDefinitions.Single().Properties.ShouldContainOnly(["versionId"]);

    record VersionRegistered([property: Unique("version", Mode = UniqueConstraintMode.PerValue)] string VersionId);
    [RemoveConstraint("version", Properties = [nameof(VersionId)])] record VersionRemoved(string VersionId);
}
