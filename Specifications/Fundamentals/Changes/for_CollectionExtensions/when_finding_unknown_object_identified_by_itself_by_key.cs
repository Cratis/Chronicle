// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Changes.for_CollectionExtensions;

public class when_finding_unknown_object_identified_by_itself_by_key : Specification
{
    IEnumerable<string> items;
    string result;

    void Establish() => items = new[]
    {
        "First",
        "Second"
    };

    void Because() => result = items.FindByKey(PropertyPath.Root, "Third");

    [Fact] void should_return_null() => result.ShouldBeNull();
}
