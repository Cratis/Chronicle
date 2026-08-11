// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// A children-collection path is written by <c>ChildAdded</c> and <c>ChildRemoved</c> and by nothing else, so the
/// initial-state diff has to leave it alone even though the initial state carries an empty collection for it. Not
/// leaving it alone is not a cosmetic difference: replay drives sibling partitions through their own events
/// independently, so a root event's <c>Members=[]</c> can arrive after a sibling's <c>ChildAdded</c> and erase a
/// child that was already there.
/// <para>
/// The exclusion argument was optional, and the one spec that exercised this method omitted it - so the branch
/// was reachable only from production. What distinguishes an excluded path from an ordinary one is the sibling
/// collection beside it, which is initial state of exactly the same shape and must still be written.
/// </para>
/// </summary>
public class when_adding_properties_and_a_children_path_is_excluded : Specification
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    ExpandoObject _source;
    List<PropertiesChanged<ExpandoObject>> _changes;

    void Establish()
    {
        _source = new ExpandoObject();
        dynamic sourceAsDynamic = _source;
        sourceAsDynamic.Name = "A name";
        sourceAsDynamic.Members = new List<ExpandoObject>();
        sourceAsDynamic.Tags = new List<ExpandoObject>();

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([]);
        _changeset.CurrentState.Returns(new ExpandoObject());

        _changes = [];
        _changeset
            .When(_ => _.Add(Arg.Any<Change>()))
            .Do(_ => _changes.Add(_.Arg<PropertiesChanged<ExpandoObject>>()));
    }

    void Because() => _changeset.AddPropertiesFrom(_source, [(PropertyPath)"Members"]);

    [Fact] void should_not_set_the_excluded_children_collection() => _changes.ShouldNotContain(_ => _.Differences.Any(difference => difference.PropertyPath == "Members"));
    [Fact] void should_still_set_a_sibling_collection() => _changes.ShouldContain(_ => _.Differences.Any(difference => difference.PropertyPath == "Tags"));
    [Fact] void should_still_set_a_scalar_property() => _changes.ShouldContain(_ => _.Differences.Any(difference => difference.PropertyPath == "Name"));
}
