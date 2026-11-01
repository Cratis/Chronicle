// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.for_ChronicleOptions;

public class when_validating_telemetry : Specification
{
    ValidationResult[] _defaultErrors;
    ValidationResult[] _omittedErrors;
    ValidationResult[] _missingKeyErrors;
    ValidationResult[] _shortKeyErrors;
    ValidationResult[] _validKeyErrors;

    void Because()
    {
        _defaultErrors = Validate(new());
        _omittedErrors = Validate(new() { EventSourceIdTracing = EventSourceIdTracing.Omitted });
        _missingKeyErrors = Validate(new() { EventSourceIdTracing = EventSourceIdTracing.Hashed });
        _shortKeyErrors = Validate(new() { EventSourceIdTracing = EventSourceIdTracing.Hashed, EventSourceIdHashKey = "short" });
        _validKeyErrors = Validate(new() { EventSourceIdTracing = EventSourceIdTracing.Hashed, EventSourceIdHashKey = "chronicle-identifier-hash-key-for-specs-0001" });
    }

    [Fact] void should_default_to_raw() => new ChronicleOptions().Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Raw);
    [Fact] void should_not_require_a_key_by_default() => _defaultErrors.ShouldBeEmpty();
    [Fact] void should_not_require_a_key_when_omitted() => _omittedErrors.ShouldBeEmpty();
    [Fact] void should_require_a_key_when_hashed() => _missingKeyErrors.Length.ShouldEqual(1);
    [Fact] void should_reject_a_short_key() => _shortKeyErrors.Length.ShouldEqual(1);
    [Fact] void should_accept_a_valid_key() => _validKeyErrors.ShouldBeEmpty();
    [Fact] void should_not_include_the_key_in_errors() => _shortKeyErrors.Single().ErrorMessage.Contains("short", StringComparison.Ordinal).ShouldBeFalse();

    static ValidationResult[] Validate(ChronicleTelemetryOptions telemetry)
    {
        var options = new ChronicleOptions { Telemetry = telemetry };
        return options.Validate(new ValidationContext(options)).ToArray();
    }
}
