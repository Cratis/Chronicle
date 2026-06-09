// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_projecting_self_referential_children_with_keyed_from_event.given;

public class a_self_ref_keyed_module_with_features(ChronicleFixture chronicleFixture) : Specification
{
    protected ChronicleFixture ChronicleFixture => chronicleFixture;

    protected EventSourceId Feature1Id { get; private set; }

    protected KeyedSelfRefSubFeatureAdded SubFeature1AddedEvent { get; private set; }
    protected KeyedSelfRefFeatureUITemplateSet Feature1TemplateSetEvent { get; private set; }

    void Establish()
    {
        Feature1Id = EventSourceId.From("feature-1");
        SubFeature1AddedEvent = new KeyedSelfRefSubFeatureAdded(Feature1Id, "Sub Feature 1");
        Feature1TemplateSetEvent = new KeyedSelfRefFeatureUITemplateSet(Feature1Id, "template-1");
    }

    protected async Task AppendFeature()
    {
        await ChronicleFixture.EventStore.EventLog.Append(Feature1Id, new KeyedSelfRefFeatureAdded("Feature 1"));
    }

    protected async Task AppendSubFeature()
    {
        await ChronicleFixture.EventStore.EventLog.Append(Feature1Id, SubFeature1AddedEvent);
    }

    protected async Task AppendTemplateUpdate()
    {
        await ChronicleFixture.EventStore.EventLog.Append(Feature1Id, Feature1TemplateSetEvent);
    }
}

[EventType]
public record KeyedSelfRefFeatureAdded(string Name);

[EventType]
public record KeyedSelfRefSubFeatureAdded(EventSourceId ParentFeatureId, string Name);

[EventType]
public record KeyedSelfRefFeatureUITemplateSet(EventSourceId FeatureId, string UITemplateId);

[FromEvent<KeyedSelfRefFeatureAdded>]
[FromEvent<KeyedSelfRefSubFeatureAdded>(parentKey: nameof(KeyedSelfRefSubFeatureAdded.ParentFeatureId))]
[FromEvent<KeyedSelfRefFeatureUITemplateSet>(key: nameof(KeyedSelfRefFeatureUITemplateSet.FeatureId))]
[ReadModel]
public record KeyedSelfRefFeature(
    string Name,
    string? UITemplateId = null,
    [ChildrenFrom<KeyedSelfRefSubFeatureAdded>(
        identifiedBy: nameof(KeyedSelfRefFeature.Name),
        parentKey: nameof(KeyedSelfRefSubFeatureAdded.ParentFeatureId))]
    IEnumerable<KeyedSelfRefFeature>? SubFeatures = null);
