// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.for_InMemoryProjectionEvents;

public class when_releasing_a_deferred_child : Engine.for_KeyResolvers.given.a_parent_key_history
{
    readonly List<(Key Key, AppendedEvent Event)> _projected = [];
    InMemoryProjectionEvents _events;
    int _beforeParent;

    async Task Establish()
    {
        await Append(0, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(1, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        await Append(2, _parentEventType, ParentSource, new { rootId = LaterRoot });
        _parent.GetKeyResolverFor(_childEventType).Returns(_resolveChild);
        _events = new InMemoryProjectionEvents(_parent, _storage, _sink, (key, @event) =>
        {
            _projected.Add((key.Key, @event));
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _events.Project(await _storage.GetEventAt(0));
        _beforeParent = _projected.Count;
        await _events.Project(await _storage.GetEventAt(1));
        await _events.Project(await _storage.GetEventAt(2));
    }

    [Fact] void should_not_project_the_child_without_a_parent() => _beforeParent.ShouldEqual(0);
    [Fact] void should_release_the_child_under_the_releasing_parent_key() => _projected.Single(_ => _.Event.Context.EventType == _childEventType).Key.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_project_the_parent_before_releasing_its_child() => _projected[0].Event.Context.EventType.ShouldEqual(_parentEventType);
    [Fact] void should_project_each_event_only_once() => _projected.Count.ShouldEqual(3);
}
