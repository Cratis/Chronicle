// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Collections.for_CollectionExtensions;

public class when_walking_enumerable_with_for_each : Specification
{
    IEnumerable<string> enumerable;
    string[] actual_enumerable =
    [
        "first",
        "second",
        "third"
    ];

    List<string> result = [];

    void Establish() => enumerable = actual_enumerable;

    void Because() => enumerable.ForEach(result.Add);

    [Fact] void should_walk_through_all_elements() => result.ShouldContainOnly(actual_enumerable);
}
