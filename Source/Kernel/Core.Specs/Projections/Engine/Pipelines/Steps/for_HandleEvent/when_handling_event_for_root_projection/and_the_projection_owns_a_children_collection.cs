// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent.when_handling_event_for_root_projection;

/// <summary>
/// The step that writes the initial state is also the step that has to withhold part of it. A children-collection
/// path belongs to <c>ChildAdded</c> and <c>ChildRemoved</c> for the read model's whole lifetime, so the diff this
/// step produces must carry no difference for it - otherwise a replayed root event <c>$set</c>s the collection
/// empty over children a sibling partition already added.
/// <para>
/// Its sibling spec asserts only that <em>some</em> change was added, which a diff that wipes the children
/// satisfies just as well as one that does not. What has to be pinned is which properties are in the diff, and
/// that needs a projection that actually owns a children collection - the fixture's root owns none, so the
/// exclusion had nothing to exclude and could be removed without a single spec turning red.
/// </para>
/// </summary>
public class and_the_projection_owns_a_children_collection : given.a_handle_event_step
{
    List<PropertiesChanged<ExpandoObject>> _changes;

    void Establish()
    {
        dynamic initialState = _projectionInitialModelState;
        initialState.Members = new List<ExpandoObject>();
        initialState.Tags = new List<ExpandoObject>();
        initialState.GroupName = "Operations";

        var childProjection = Substitute.For<IProjection>();
        childProjection.Path.Returns(new ProjectionPath("TestProjection.Members"));
        childProjection.ChildrenPropertyPath.Returns(new PropertyPath("Members"));
        childProjection.ChildProjections.Returns([]);

        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _projection.ChildProjections.Returns([childProjection]);
        _projection.Accepts(_event.Context.EventType).Returns(true);

        _changes = [];
        _changeset
            .When(_ => _.Add(Arg.Any<Change>()))
            .Do(_ => _changes.Add(_.Arg<PropertiesChanged<ExpandoObject>>()));

        _context = _context with { NeedsInitialState = true };
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_not_set_the_children_collection_from_the_initial_state() => _changes.ShouldNotContain(_ => _.Differences.Any(difference => difference.PropertyPath == "Members"));
    [Fact] void should_still_set_a_collection_it_does_not_own() => _changes.ShouldContain(_ => _.Differences.Any(difference => difference.PropertyPath == "Tags"));
    [Fact] void should_still_set_a_scalar_property() => _changes.ShouldContain(_ => _.Differences.Any(difference => difference.PropertyPath == "GroupName"));
}
