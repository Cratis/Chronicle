// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_caused_by_is_the_unknown_sentinel : given.a_metadata_query
{
    void Establish() => _storedIdentities[_identityId] = Concepts.Identities.Identity.Unknown;

    async Task Because() => await Read(42);

    [Fact] void should_report_unknown_initiator() => _result.Single().InitiatorType.ShouldEqual(InitiatorType.Unknown);
}
