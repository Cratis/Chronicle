// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

public abstract class and_observing_a_page<THarness> : a_populated_sink<THarness>
    where THarness : ISinkHarness, new()
{
    string[] _names;

    async Task Because()
    {
        var instances = await _sink.ObserveInstances(skip: 1, take: 2).FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10));
        _names = instances.Select(instance => (string)((IDictionary<string, object?>)instance)["name"]!).ToArray();
    }

    [Fact] public void should_order_the_page_by_key() => _names.ShouldEqual(["b", "c"]);
}
