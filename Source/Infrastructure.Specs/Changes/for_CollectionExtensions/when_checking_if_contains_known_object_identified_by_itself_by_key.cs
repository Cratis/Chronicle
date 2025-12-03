// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_checking_if_contains_known_object_identified_by_itself_by_key : Specification
{
    IEnumerable<string> _items;
    bool _result;

    void Establish() => _items =
    [
        "First",
        "Second"
    ];

    void Because() => _result = _items.Contains(PropertyPath.Root, "Second");

    [Fact] void should_contain_the_object() => _result.ShouldBeTrue();
}
