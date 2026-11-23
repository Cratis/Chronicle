// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

public class when_adding_properties_and_nested_resolved_joins_have_changes : Specification
{
    Changeset<AppendedEvent, ExpandoObject> _changeset;
    ExpandoObject _source;
    IEnumerable<PropertyDifference> _initialDifferences;

    void Establish()
    {
        var incoming = new AppendedEvent(EventContext.Empty, new ExpandoObject());
        var state = new ExpandoObject();
        _source = new { name = "seed", nullable = "seed", direct = "seed", status = "pending", children = Array.Empty<ExpandoObject>() }.AsExpandoObject();
        _changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        _changeset.Add(new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference("direct", "seed", "changed")]));
        _changeset.Add(new ChildAdded(state, "[children]", "id", "child", ArrayIndexers.NoIndexers));
        var join = _changeset.ResolvedJoin("customerId", "customer", incoming, ArrayIndexers.NoIndexers);
        join.Add(new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference("name", "seed", "Alice")]));
        var nestedJoin = join.ResolvedJoin("otherId", "other", incoming, ArrayIndexers.NoIndexers);
        nestedJoin.Add(new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference("nullable", "seed", null)]));
    }

    void Because()
    {
        _changeset.AddPropertiesFrom(_source);
        _initialDifferences = _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Last().Differences;
    }

    [Fact] void should_not_add_a_default_for_a_joined_property() => _initialDifferences.ShouldNotContain(_ => _.PropertyPath == "name");
    [Fact] void should_not_add_a_default_for_a_nested_joined_null() => _initialDifferences.ShouldNotContain(_ => _.PropertyPath == "nullable");
    [Fact] void should_not_add_a_default_for_a_direct_change() => _initialDifferences.ShouldNotContain(_ => _.PropertyPath == "direct");
    [Fact] void should_not_replace_a_child_collection() => _initialDifferences.ShouldNotContain(_ => _.PropertyPath == "children");
    [Fact] void should_still_add_unmapped_defaults() => _initialDifferences.Single().Changed.ShouldEqual("pending");
}
