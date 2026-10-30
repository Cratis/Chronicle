// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScope;

public class when_scope_metadata_is_copied : Specification
{
    ConcurrencyScope _original;
    ConcurrencyScope _copy;
    Dictionary<ConcurrencyScope, string> _lookup;

    void Establish()
    {
        _original = new(42UL, "source", "stream", EventTypes: [new("event", EventTypeGeneration.First)]);
        _lookup = new() { [_original] = "found" };
    }

    void Because() => _copy = _original with { IssuedByStrategy = true, DefinitionType = typeof(string) };

    [Fact] void should_preserve_value_equality() => _copy.ShouldEqual(_original);
    [Fact] void should_preserve_dictionary_lookup() => _lookup[_copy].ShouldEqual("found");
    [Fact] void should_preserve_strategy_provenance_in_a_with_copy() => (_copy with { SequenceNumber = EventSequenceNumber.Unavailable }).IssuedByStrategy.ShouldBeTrue();
    [Fact] void should_preserve_definition_link_in_a_with_copy() => (_copy with { SequenceNumber = EventSequenceNumber.Unavailable }).DefinitionType.ShouldEqual(typeof(string));
    [Fact] void should_preserve_the_mutation_epoch_in_a_with_copy() => (_original with { MutationEpoch = 6UL } with { SequenceNumber = EventSequenceNumber.Unavailable }).MutationEpoch.ShouldEqual(6UL);
    [Fact] void should_keep_the_mutation_epoch_outside_value_equality() => (_original with { MutationEpoch = 6UL }).ShouldEqual(_original);
    [Fact] void should_preserve_collection_reference_equality() => (_original with { EventTypes = _original.EventTypes!.ToArray() }).ShouldNotEqual(_original);
}
