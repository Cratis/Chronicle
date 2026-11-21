// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkEvents;

public class when_creating_the_event_types_storage : Specification
{
    bool _selected;
    bool _other;
    EventType _resolved = default!;
    async Task Because()
    {
        var types = BenchmarkEvents.CreateEventTypes();
        _selected = await types.HasFor(BenchmarkEvents.Selected.Id, BenchmarkEvents.Selected.Generation);
        _other = await types.HasFor(BenchmarkEvents.Other.Id, BenchmarkEvents.Other.Generation);
        _resolved = (await types.GetFor(BenchmarkEvents.Other.Id, BenchmarkEvents.Other.Generation)).Type;
    }
    [Fact] void should_register_the_selected_type() => _selected.ShouldBeTrue();
    [Fact] void should_register_the_other_type() => _other.ShouldBeTrue();
    [Fact] void should_resolve_the_requested_type() => _resolved.Id.ShouldEqual(BenchmarkEvents.Other.Id);
}
