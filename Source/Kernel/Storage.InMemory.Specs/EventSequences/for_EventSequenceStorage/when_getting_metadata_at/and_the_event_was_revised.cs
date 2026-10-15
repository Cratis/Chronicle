// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_metadata_at;

public class and_the_event_was_revised : given.a_metadata_storage
{
    async Task Establish()
    {
        await _storage.AppendMany([_entry]);
        await _storage.Revise(42UL, new EventType("revision-type", 2), CorrelationId.New(), [], [IdentityId.New()], DateTimeOffset.UtcNow.AddMinutes(1), new ExpandoObject(), EventHash.NotSet);
    }

    async Task Because() => await Read();

    [Fact] void should_keep_the_original_type_id_like_persistent_providers() => _result.EventTypeId.ShouldEqual(_entry.EventType.Id);
    [Fact] void should_keep_the_original_time() => _result.Occurred.ShouldEqual(_entry.Occurred);
    [Fact] void should_keep_the_original_correlation() => _result.CorrelationId.ShouldEqual(_entry.CorrelationId);
    [Fact] void should_keep_the_original_causation() => _result.Causation.Single().Type.ShouldEqual(_entry.Causation.Single().Type);
    [Fact] void should_keep_the_original_causation_properties() => _result.Causation.Single().Properties["value"].ShouldEqual("original");
    [Fact] void should_keep_the_original_identity_chain() => _result.CausedByChain.ShouldContainOnly(_entry.CausedByChain);
    [Fact] void should_not_resolve_or_create_identities_during_reads() => _identities.ReceivedCalls().ShouldBeEmpty();
}
