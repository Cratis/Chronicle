// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Projections.for_KeyResolvers;

public class when_identifying_model_key_from_parent_hierarchy_with_four_levels : Specification
{
    AppendedEvent root_event;
    AppendedEvent first_level_event;
    AppendedEvent second_level_event;
    AppendedEvent third_level_event;
    AppendedEvent forth_level_event;

    Mock<IProjection> root_projection;
    Mock<IProjection> first_level_projection;
    Mock<IProjection> second_level_projection;
    Mock<IProjection> third_level_projection;
    Mock<IProjection> forth_level_projection;

    Mock<IEventSequenceStorage> storage;
    Key result;

    static EventType root_event_type = new("5f4f4368-6989-4d9d-a84e-7393e0b41cfd", 1);
    static EventType first_level_event_type = new("eef0f7c0-25eb-48dc-b824-7e27ba4593f2", 1);
    static EventType second_level_event_type = new("6682281b-64e0-431b-8b90-dd49ba25ca55", 1);
    static EventType third_level_event_type = new("90ae84f7-84f0-4e3a-a38a-329d782da158", 1);
    static EventType forth_level_event_type = new("e72686b1-b0e4-40a8-9651-4841602638da", 1);

    const string root_key = "4e3a1e36-714c-41f7-83e3-5cc84717db16";
    const string first_level_key = "805908d8-ed72-4a70-b313-6f592632663d";
    const string second_level_key = "02311df8-fcf2-42ff-b2d0-b7fa2f576485";
    const string third_level_key = "935e1158-cedb-4530-a9aa-d925d6d9b10d";
    const string forth_level_key = "537d661f-8b12-4a1e-917b-faf639923380";

    AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber, EventType type, EventSourceId eventSourceId, object content)
    {
        return new(
            new(sequenceNumber, type),
            new(eventSourceId, sequenceNumber, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            content.AsExpandoObject());
    }

    Mock<IProjection> SetupProjection(EventType eventType, string key, string childrenProperty = "no-levels", IProjection? parent = null)
    {
        var projection = new Mock<IProjection>();
        projection.SetupGet(_ => _.EventTypes).Returns(new[] { eventType });
        projection.SetupGet(_ => _.OwnEventTypes).Returns(new[] { eventType });
        projection.SetupGet(_ => _.Path).Returns(childrenProperty);
        projection.SetupGet(_ => _.ChildrenPropertyPath).Returns(childrenProperty);

        if (parent is not null)
        {
            projection.Setup(_ => _.GetKeyResolverFor(eventType)).Returns(KeyResolvers.FromParentHierarchy(
                projection.Object,
                KeyResolvers.FromEventSourceId,
                KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
                "childId"));
            projection.SetupGet(_ => _.HasParent).Returns(true);
            projection.SetupGet(_ => _.Parent).Returns(parent);
        }
        else
        {
            projection.Setup(_ => _.GetKeyResolverFor(eventType)).Returns((_, __) => Task.FromResult(new Key(key, ArrayIndexers.NoIndexers)));
        }
        return projection;
    }

    void Establish()
    {
        root_event = CreateEvent(0, root_event_type, root_key, new { });
        first_level_event = CreateEvent(1, first_level_event_type, first_level_key, new { parentId = root_key });
        second_level_event = CreateEvent(2, second_level_event_type, second_level_key, new { parentId = first_level_key });
        third_level_event = CreateEvent(3, third_level_event_type, third_level_key, new { parentId = second_level_key });
        forth_level_event = CreateEvent(4, forth_level_event_type, forth_level_key, new { parentId = third_level_key });

        root_projection = SetupProjection(root_event_type, root_key);
        first_level_projection = SetupProjection(first_level_event_type, first_level_key, "firstLevels", root_projection.Object);
        second_level_projection = SetupProjection(second_level_event_type, second_level_key, "secondLevels", first_level_projection.Object);
        third_level_projection = SetupProjection(third_level_event_type, third_level_key, "thirdLevels", second_level_projection.Object);
        forth_level_projection = SetupProjection(forth_level_event_type, forth_level_key, "forthLevels", third_level_projection.Object);

        storage = new();
        storage.Setup(_ => _.GetLastInstanceOfAny(root_key, new[] { root_event_type.Id })).Returns(Task.FromResult(root_event));
        storage.Setup(_ => _.GetLastInstanceOfAny(first_level_key, new[] { first_level_event_type.Id })).Returns(Task.FromResult(first_level_event));
        storage.Setup(_ => _.GetLastInstanceOfAny(second_level_key, new[] { second_level_event_type.Id })).Returns(Task.FromResult(second_level_event));
        storage.Setup(_ => _.GetLastInstanceOfAny(third_level_key, new[] { third_level_event_type.Id })).Returns(Task.FromResult(third_level_event));
        storage.Setup(_ => _.GetLastInstanceOfAny(forth_level_key, new[] { forth_level_event_type.Id })).Returns(Task.FromResult(forth_level_event));
    }

    async Task Because() => result = await KeyResolvers.FromParentHierarchy(
        forth_level_projection.Object,
        KeyResolvers.FromEventSourceId,
        KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
        "childId")(storage.Object, forth_level_event);

    [Fact] void should_return_expected_key() => result.Value.ShouldEqual(root_key);
    [Fact] void should_hold_array_indexer_for_first_level_with_correct_identifier() => result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "firstLevels").Identifier.ToString().ShouldEqual(first_level_key);
    [Fact] void should_hold_array_indexer_for_second_level_with_correct_identifier() => result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "secondLevels").Identifier.ToString().ShouldEqual(second_level_key);
    [Fact] void should_hold_array_indexer_for_third_level_with_correct_identifier() => result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "thirdLevels").Identifier.ToString().ShouldEqual(third_level_key);
    [Fact] void should_hold_array_indexer_for_forth_level_with_correct_identifier() => result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "forthLevels").Identifier.ToString().ShouldEqual(forth_level_key);
}
