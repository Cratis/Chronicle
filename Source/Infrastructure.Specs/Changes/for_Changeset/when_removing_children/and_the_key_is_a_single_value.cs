// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_removing_children;

public class and_the_key_is_a_single_value : Specification
{
    Changeset<object, ExpandoObject> _changeset;

    void Establish()
    {
        var state = new
        {
            items = new[] { new { id = "other" }, new { id = "target" } }
        }.AsExpandoObject();
        _changeset = new(Substitute.For<IObjectComparer>(), new object(), state);
    }

    void Because() => _changeset.RemoveChild("[items]", "id", "target", ArrayIndexers.NoIndexers);

    [Fact] void should_record_one_child_removal() => _changeset.Changes.Single().ShouldBeOfExactType<ChildRemoved>();
    [Fact] void should_record_the_matching_child() => ((IDictionary<string, object?>)_changeset.Changes.OfType<ChildRemoved>().Single().Child)["id"].ShouldEqual("target");
    [Fact] void should_keep_the_other_child() => ((IDictionary<string, object?>)((IEnumerable<object>)((IDictionary<string, object?>)_changeset.CurrentState)["items"]!).Single())["id"].ShouldEqual("other");
}
