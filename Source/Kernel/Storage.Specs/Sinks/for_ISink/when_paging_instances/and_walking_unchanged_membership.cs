// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

public abstract class and_walking_unchanged_membership<THarness> : a_populated_sink<THarness>
    where THarness : ISinkHarness, new()
{
    readonly List<string> _names = [];

    async Task Because()
    {
        for (var skip = 0; skip < 5; skip += 2)
        {
            _names.AddRange(Names(await _sink.GetInstances(skip: skip, take: 2)));
        }
    }

    [Fact] public void should_visit_each_instance_exactly_once() => _names.ShouldEqual(["a", "b", "c", "d", "e"]);
}
