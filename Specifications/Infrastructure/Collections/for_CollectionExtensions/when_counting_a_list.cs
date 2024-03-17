// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Collections.for_CollectionExtensions;

public class when_counting_a_list : Specification
{
    int count;

    void Because() => count = new List<int> { 1, 2, 3 }.CountElements();

    [Fact] void should_have_correct_count() => count.ShouldEqual(3);
}
