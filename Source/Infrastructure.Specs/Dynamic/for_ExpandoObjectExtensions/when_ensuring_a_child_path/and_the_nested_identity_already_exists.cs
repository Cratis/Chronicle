// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_a_child_path;

public class and_the_nested_identity_already_exists : Specification
{
    ExpandoObject _state;
    ExpandoObject _child;
    ExpandoObject _result;
    ExpandoObject? _existing;
    readonly PropertyPath _path = "[children].topic";
    readonly ArrayIndexers _indexers = new([new ArrayIndexer("[children]", "identity.id", "A")]);

    void Establish()
    {
        dynamic child = new ExpandoObject();
        child.identity = new ExpandoObject();
        child.identity.id = "A";
        _child = child;
        dynamic state = new ExpandoObject();
        state.children = new List<ExpandoObject> { _child };
        _state = state;
    }

    void Because()
    {
        _result = _state.EnsurePath(_path, _indexers);
        _existing = _state.TryGetExistingPath(_path, _indexers);
    }

    [Fact] void should_reuse_the_child() => _result.ShouldEqual(_child);
    [Fact] void should_find_the_existing_path() => _existing.ShouldEqual(_child);
    [Fact] void should_not_add_a_child() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).Count().ShouldEqual(1);
}
