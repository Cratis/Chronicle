// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_removing_a_container;

/// <summary>
/// Removing a container other than the one the read model lives in, a revert container say, leaves the primary
/// collection alone, so nothing is announced to the observers of it.
/// </summary>
public class and_it_is_not_the_primary_collection : a_populated_sink<InMemorySinkHarness>
{
    int _emissions;
    int _emissionsAfterRemoving;

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances(take: 10).Subscribe(_ => _emissions++);
        await _sink.Remove("paged_read_models-revert");
        _emissionsAfterRemoving = _emissions;
    }

    [Fact] void should_only_have_emitted_the_first_page() => _emissionsAfterRemoving.ShouldEqual(1);
}
