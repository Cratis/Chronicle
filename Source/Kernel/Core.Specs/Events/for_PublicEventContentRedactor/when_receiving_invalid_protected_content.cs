// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_receiving_invalid_protected_content : Specification
{
    string[] _inputs;
    Exception?[] _errors;

    void Establish() => _inputs = ["current-header-value", "{\"targetHeaders\":\"original-header-value\"", "[\"revision-one-header-value\"]", "null", "\"revision-two-header-value\""];

    void Because() => _errors = _inputs.Select(content => Catch.Exception(() => PublicEventContentRedactor.Redact(new EventType("WebhookAdded", 1), content, given.header_events.Options))).ToArray();

    [Fact] void should_fail_closed() => _errors.All(error => error is JsonException).ShouldBeTrue();
    [Fact] void should_use_neutral_errors() => _errors.All(error => error is not null && error.Message == "Invalid public event content." && error.InnerException is null).ShouldBeTrue();
    [Fact] void should_not_include_content_in_exception_text() => _errors.All(error => error is not null && !given.header_events.ContainsSentinel(error.ToString())).ShouldBeTrue();
}
