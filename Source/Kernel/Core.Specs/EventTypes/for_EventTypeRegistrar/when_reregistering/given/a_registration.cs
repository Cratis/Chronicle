// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering.given;

public class a_registration : for_EventTypeRegistrar.given.all_dependencies
{
    protected const string DefaultOnly = """{"type":"object","properties":{"amount":{"default":null}}}""";
    protected const string Typed = """{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null}}}""";
    protected const string Protected = """{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null,"compliance":[{"metadataType":"PII","details":""}]}}}""";
    protected EventTypeToRegister _registered;

    void Establish() => _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns(call =>
    {
        _registered = call.Arg<IEnumerable<EventTypeToRegister>>().Single();
        return [];
    });

    protected Task Register(string schema, bool skipValidation = false) => _subject.Register(
        "test-store",
        [new EventTypeRegistration { Type = new() { Id = "decimal", Generation = 1 }, Schema = schema }],
        skipValidation,
        _storage,
        _eventTypesCacheClient,
        _patternCapture);
}
