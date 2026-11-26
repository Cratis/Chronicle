// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_removing_children;

public class and_the_child_is_identified_by_itself : Specification
{
    Changeset<object, ExpandoObject> _changeset;

    void Establish()
    {
        var state = new { items = new[] { "other", "target" } }.AsExpandoObject();
        _changeset = new(Substitute.For<IObjectComparer>(), new object(), state);
    }

    void Because() => _changeset.RemoveChild("[items]", PropertyPath.Root, "target", ArrayIndexers.NoIndexers);

    [Fact] void should_record_the_matching_child() => _changeset.Changes.OfType<ChildRemoved>().Single().Child.ShouldEqual("target");
    [Fact] void should_keep_the_other_child() => ((IEnumerable<object>)((IDictionary<string, object?>)_changeset.CurrentState)["items"]!).ShouldEqual(["other"]);
}
