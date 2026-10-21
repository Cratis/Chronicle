// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/4660 — only the details text is free to change.
/// </summary>
public class and_schema_compliance_details_differ : given.all_dependencies
{
    const string StoredSchema = """{"type":"object","properties":{"amount":{"type":"string","compliance":[{"metadataType":"PII","details":"Old text"}]}}}""";
    const string IncomingSchema = """{"type":"object","properties":{"amount":{"type":"string","compliance":[{"metadataType":"PII","details":"New text"}]}}}""";

    Exception _exception;

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, StoredSchema)));

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1 },
                    Schema = IncomingSchema,
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = IncomingSchema }
                    }
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
