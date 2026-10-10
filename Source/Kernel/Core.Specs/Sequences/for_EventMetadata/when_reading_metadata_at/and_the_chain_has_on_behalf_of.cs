// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_the_chain_has_on_behalf_of : given.a_metadata_query
{
    void Establish()
    {
        var userId = IdentityId.New();
        _storedIdentities[userId] = new("person", "Person name", "person-user");
        _entry = _entry with { CausedByChain = [_identityId, userId] };
    }

    async Task Because() => await Read(42);

    [Fact] void should_report_an_agent() => _result.Single().InitiatorType.ShouldEqual(InitiatorType.Agent);
    [Fact] void should_resolve_the_chain_head() => _result.Single().CausedBy.Name.ShouldEqual("Current name");
    [Fact] void should_resolve_on_behalf_of() => _result.Single().CausedBy.OnBehalfOf.Name.ShouldEqual("Person name");
    [Fact] void should_end_the_chain() => _result.Single().CausedBy.OnBehalfOf.OnBehalfOf.ShouldBeNull();
}
