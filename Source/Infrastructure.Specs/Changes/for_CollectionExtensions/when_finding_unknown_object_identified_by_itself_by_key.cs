// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_finding_unknown_object_identified_by_itself_by_key : Specification
{
    IEnumerable<string> _items;
    string _result;

    void Establish() => _items =
    [
        "First",
        "Second"
    ];

    void Because() => _result = _items.FindByKey(PropertyPath.Root, "Third");

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
