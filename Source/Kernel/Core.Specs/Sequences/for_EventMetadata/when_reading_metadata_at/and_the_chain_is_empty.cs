// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_the_chain_is_empty : given.a_metadata_query
{
    void Establish() => _entry = _entry with { CausedByChain = [] };

    async Task Because() => await Read(42);

    [Fact] void should_report_not_set() => _result.Single().CausedBy.Resolution.ShouldEqual(IdentityResolution.NotSet);
    [Fact] void should_not_invent_a_name() => _result.Single().CausedBy.Name.ShouldBeNull();
    [Fact] void should_report_unknown_initiator() => _result.Single().InitiatorType.ShouldEqual(InitiatorType.Unknown);
}
