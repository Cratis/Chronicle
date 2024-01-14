// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Projections.for_KeyResolvers;

public class when_identifying_model_key_from_parent_hierarchy_with_one_level : Specification
{
    AppendedEvent root_event;
    AppendedEvent @event;
    Key result;
    Mock<IProjection> root_projection;
    Mock<IProjection> child_projection;
    Mock<IEventSequenceStorage> storage;

    static EventType root_event_type = new("5f4f4368-6989-4d9d-a84e-7393e0b41cfd", 1);
    const string parent_key = "61fcc353-3478-4cf9-a783-da508013b36f";

    void Establish()
    {
        root_event = new(
            new(1, root_event_type),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            new ExpandoObject());

        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            new
            {
                parentId = parent_key
            }.AsExpandoObject());

        root_projection = new();
        root_projection.SetupGet(_ => _.EventTypes).Returns(new EventType[]
        {
            root_event_type
        });

        child_projection = new();
        child_projection.SetupGet(_ => _.HasParent).Returns(true);
        child_projection.SetupGet(_ => _.Parent).Returns(root_projection.Object);
        child_projection.SetupGet(_ => _.ChildrenPropertyPath).Returns("children");
        storage = new();

        storage.Setup(_ => _.GetLastInstanceOfAny(parent_key, new[] { root_event_type.Id })).Returns(Task.FromResult(root_event));
        root_projection.Setup(_ => _.GetKeyResolverFor(root_event_type)).Returns((_, __) => Task.FromResult(new Key(parent_key, ArrayIndexers.NoIndexers)));
    }

    async Task Because() => result = await KeyResolvers.FromParentHierarchy(
        child_projection.Object,
        KeyResolvers.FromEventSourceId,
        KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
        "childId")(storage.Object, @event);

    [Fact] void should_return_expected_key() => result.Value.ShouldEqual(parent_key);
}
