// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorage;

public class when_checking_own_namespace_data : Specification
{
    EventStoreStorage _store;
    bool _exact;
    bool _variant;
    bool _missing;

    void Establish()
    {
        _store = new("Studio", _ => Substitute.For<ISinks>(), Substitute.For<Cratis.Choreography.Storage.IJobsStorage>());
        _store.GetNamespace("Cratis");
    }

    async Task Because()
    {
        _exact = await _store.HasOwnNamespaceData("Cratis");
        _variant = await _store.HasOwnNamespaceData("cratis");
        _missing = await _store.HasOwnNamespaceData("missing");
    }

    [Fact] void should_find_the_exact_name() => _exact.ShouldBeTrue();
    [Fact] void should_not_find_a_case_variant() => _variant.ShouldBeFalse();
    [Fact] void should_not_find_an_unknown_name() => _missing.ShouldBeFalse();
}
