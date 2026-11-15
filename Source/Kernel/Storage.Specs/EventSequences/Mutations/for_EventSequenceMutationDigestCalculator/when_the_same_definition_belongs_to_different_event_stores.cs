// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_the_same_definition_belongs_to_different_event_stores : given.a_digest_calculation
{
    EventSequenceMutationDigestVersion _version;
    EventSequenceMutationDefinitionDigestV1 _first;
    EventSequenceMutationDefinitionDigestV1 _second;

    void Because()
    {
        _version = EventSequenceMutationDigestCalculator.CurrentDigestVersion;
        _first = CalculateDefinition(new("event-log", "first-store", "default"));
        _second = CalculateDefinition(new("event-log", "second-store", "default"));
    }

    [Fact] void should_write_with_the_keyed_version() => _version.ShouldEqual(EventSequenceMutationDigestVersion.HmacSha256PerEventStore);
    [Fact] void should_produce_different_digests() => _first.ShouldNotEqual(_second);
}
