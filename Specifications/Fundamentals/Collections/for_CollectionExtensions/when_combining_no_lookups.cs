﻿// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Collections.for_CollectionExtensions;

public class when_combining_no_lookups : Specification
{
    IEnumerable<ILookup<string, int>> lookups;
    ILookup<string, int> result;

    void Establish() => lookups = Enumerable.Empty<ILookup<string, int>>();

    void Because() => result = lookups.Combine();

    [Fact] void should_be_empty() => result.Count.ShouldEqual(0);
}
