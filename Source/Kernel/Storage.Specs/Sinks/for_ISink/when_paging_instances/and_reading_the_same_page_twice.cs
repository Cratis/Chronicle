// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

public abstract class and_reading_the_same_page_twice<THarness> : a_populated_sink<THarness>
    where THarness : ISinkHarness, new()
{
    string[] _first;
    string[] _second;

    async Task Because()
    {
        _first = Names(await _sink.GetInstances(skip: 1, take: 2));
        _second = Names(await _sink.GetInstances(skip: 1, take: 2));
    }

    [Fact] public void should_return_the_same_page() => _second.ShouldEqual(_first);
    [Fact] public void should_order_the_page_by_key() => _first.ShouldEqual(["b", "c"]);
}
