// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting;

public class and_the_event_is_already_redacted : given.a_scenario_with_notes
{
    Exception _error;
    JsonElement _payload;

    async Task Establish() => await _scenario.EventSequence.Redact(_firstNote, "first");

    async Task Because()
    {
        _error = await Catch.Exception(() => _scenario.EventSequence.Redact(_firstNote, "second"));
        _payload = JsonDocument.Parse(await _scenario.ReadContentAtRest(_firstNote)).RootElement;
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_keep_the_first_reason() => _payload.GetProperty("reason").GetString().ShouldEqual("first");
}
