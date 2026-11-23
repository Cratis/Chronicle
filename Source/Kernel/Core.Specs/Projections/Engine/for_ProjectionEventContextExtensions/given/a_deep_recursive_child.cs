// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.given;

public class a_deep_recursive_child : a_projection_event_context_observable
{
    protected static readonly PropertyPath _childrenPath = "[features].[subFeatures]";
    protected Changeset<AppendedEvent, ExpandoObject> _changeset;

    void Establish()
    {
        dynamic state = new ExpandoObject();
        state.features = new List<ExpandoObject> { Feature("root", Feature("first", Feature("second", Feature("third"), Feature("sibling")))) };
        _changeset = new(new ObjectComparer(), _eventContext.Event, (ExpandoObject)state);
        Target("added");
    }

    protected void Target(string id) => _eventContext = _eventContext with
    {
        Key = new Key("module", new ArrayIndexers(
        [
            new("[features]", "id", "root"),
            new(_childrenPath, "id", "first"),
            new(_childrenPath, "id", "second"),
            new(_childrenPath, "id", id)
        ])),
        Changeset = _changeset
    };

    protected ExpandoObject Root => Children(_changeset.CurrentState, "features").Single();
    protected ExpandoObject First => Children(Root, "subFeatures").Single();
    protected ExpandoObject Second => Children(First, "subFeatures").Single();
    protected IEnumerable<ExpandoObject> Leaves => Children(Second, "subFeatures");

    protected static object Value(ExpandoObject feature, string property) => ((IDictionary<string, object?>)feature)[property]!;

    protected static IEnumerable<ExpandoObject> Children(ExpandoObject feature, string property) => ((IEnumerable<object>)Value(feature, property)).Cast<ExpandoObject>();

    static ExpandoObject Feature(string id, params ExpandoObject[] children)
    {
        dynamic feature = new ExpandoObject();
        feature.id = id;
        feature.name = id;
        feature.subFeatures = children.ToList();
        return feature;
    }
}
