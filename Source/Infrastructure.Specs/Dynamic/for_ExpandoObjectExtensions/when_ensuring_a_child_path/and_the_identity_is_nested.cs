// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_a_child_path;

public class and_the_identity_is_nested : Specification
{
    ExpandoObject _state;
    ExpandoObject _child;

    void Establish() => _state = new ExpandoObject();

    void Because() => _child = _state.EnsurePath(
        new PropertyPath("[children].topic"),
        new ArrayIndexers([new ArrayIndexer("[children]", "identity.id", "A")]));

    [Fact] void should_create_the_nested_identity() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_child)["identity"]!)["id"].ShouldEqual("A");
    [Fact] void should_not_create_a_dotted_literal_key() => ((IDictionary<string, object?>)_child).ContainsKey("identity.id").ShouldBeFalse();
}
