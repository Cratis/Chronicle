// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_releasing_a_bounded_parent_to_its_pinned_generation : given.a_parent_key_history
{
    readonly List<ulong> _released = [];
    Key _key;

    void Establish()
    {
        _parent.GenerationDelivery.Returns(EventGenerationDelivery.Pinned);
        _parent.ReleaseEvent(_parentEventType, Arg.Any<AppendedEvent>()).Returns(call =>
        {
            var candidate = call.ArgAt<AppendedEvent>(1);
            _released.Add(candidate.Context.SequenceNumber.Value);
            return candidate with
            {
                Context = candidate.Context with { EventType = _parentEventType },
                Content = new { rootId = "pinned-root" }.AsExpandoObject()
            };
        });
    }

    async Task Because()
    {
        await Append(0, _parentEventType with { Generation = new EventTypeGeneration(2) }, ParentSource, new { rootId = "unreleased-root" });
        await Append(1, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(2, _parentEventType, ParentSource, new { rootId = LaterRoot });
        _key = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(1))).Key;
    }

    [Fact] void should_resolve_using_the_projection_pin() => _key.Value.ShouldEqual("pinned-root");
    [Fact] void should_never_release_a_parent_from_after_the_child() => _released.ShouldEachConformTo(number => number == 0);
    [Fact] void should_have_released_a_candidate() => _released.Count.ShouldBeGreaterThan(0);
}
