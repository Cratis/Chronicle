// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkEvents;

/// <summary>Verifies that schema and identity lookups reuse fixed results.</summary>
public class when_resolving_fixed_fixtures : Specification
{
    EventTypeSchema _selected = null!;
    EventTypeSchema _selectedAgain = null!;
    EventTypeSchema _other = null!;
    Identity _identity = null!;
    Identity _identityAgain = null!;

    async Task Because()
    {
        var types = BenchmarkEvents.CreateEventTypes();
        _selected = await types.GetFor(BenchmarkEvents.Selected.Id);
        _selectedAgain = await types.GetFor(BenchmarkEvents.Selected.Id, BenchmarkEvents.Selected.Generation);
        _other = await types.GetFor(BenchmarkEvents.Other.Id);
        var identities = BenchmarkEvents.CreateIdentities();
        _identity = await identities.GetFor(Array.Empty<IdentityId>());
        _identityAgain = await identities.GetFor(Array.Empty<IdentityId>());
    }

    [Fact] void should_reuse_the_selected_schema() => ReferenceEquals(_selected, _selectedAgain).ShouldBeTrue();
    [Fact] void should_resolve_the_default_generation() => _other.Type.ShouldEqual(BenchmarkEvents.Other);
    [Fact] void should_keep_the_schema_empty() => _selected.Schema.Properties.Count.ShouldEqual(0);
    [Fact] void should_resolve_system_identity() => _identity.ShouldEqual(Identity.System);
    [Fact] void should_reuse_identity() => ReferenceEquals(_identity, _identityAgain).ShouldBeTrue();
}
