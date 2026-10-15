// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_redacting;

public class and_generations_were_added : given.a_storage_with_stored_generations
{
    async Task Establish() => (await _storage.TryAddGenerations(_observed, [Target()])).ShouldBeTrue();

    async Task Because() => await _storage.Redact(0, "reason", CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow);

    [Fact] void should_clear_the_generation_hashes() => _storage.GetGenerationHashes(0).ShouldBeEmpty();
    [Fact] void should_clear_the_derived_generations() => _storage.GetDerivedGenerations(0).ShouldBeEmpty();
}
