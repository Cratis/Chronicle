// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_revising_generational_content;

public class and_the_current_generation_is_not_the_first : given.a_storage_for_appended_generation
{
    AppendedEvent _result;

    async Task Establish() => (await _storage.AppendMany([EventAt(0, 2)])).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        await _storage.Revise(0, _eventType with { Generation = 2 }, CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow, new ExpandoObject(), "revised-hash");
        _result = await _storage.GetEventAt(0);
    }

    [Fact] void should_report_original_content_from_the_first_generation() => JsonNode.Parse(_result.OriginalContent)!["value"]!.GetValue<string>().ShouldEqual("original");
    [Fact] void should_keep_the_appended_generation() => _result.Context.AppendedGeneration!.Value.ShouldEqual(2U);
}
