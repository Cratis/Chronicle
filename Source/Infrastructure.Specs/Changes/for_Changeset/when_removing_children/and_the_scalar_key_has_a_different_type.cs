// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_removing_children;

public class and_the_scalar_key_has_a_different_type : Specification
{
    Changeset<object, ExpandoObject> _changeset;

    void Establish()
    {
        var state = new { items = new[] { new { id = 42 } } }.AsExpandoObject();
        _changeset = new(Substitute.For<IObjectComparer>(), new object(), state);
    }

    void Because() => _changeset.RemoveChild("[items]", "id", 42L, ArrayIndexers.NoIndexers);

    [Fact] void should_not_coerce_the_scalar_key() => _changeset.Changes.ShouldBeEmpty();
    [Fact] void should_keep_the_child() => ((IEnumerable<object>)((IDictionary<string, object?>)_changeset.CurrentState)["items"]!).Count().ShouldEqual(1);
}
