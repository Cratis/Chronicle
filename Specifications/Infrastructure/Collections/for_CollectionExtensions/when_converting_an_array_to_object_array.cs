// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Collections.for_CollectionExtensions;

public class when_converting_an_array_to_object_array : Specification
{
    object[] result;

    void Because() => result = new[] { 1, 2, 3 }.ToObjectArray();

    [Fact] void should_have_correct_count() => result.ShouldContainOnly(1, 2, 3);
}
