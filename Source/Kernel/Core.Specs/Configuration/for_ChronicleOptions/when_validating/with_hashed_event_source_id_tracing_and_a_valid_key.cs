// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class with_hashed_event_source_id_tracing_and_a_valid_key : Specification
{
    IEnumerable<ValidationResult> _errors;

    void Because()
    {
        var options = new ChronicleOptions { Telemetry = new() { EventSourceIdTracing = EventSourceIdTracing.Hashed, EventSourceIdHashKey = new string('x', 32) } };
        _errors = options.Validate(new ValidationContext(options)).ToArray();
    }

    [Fact] void should_accept_the_hash_key() => _errors.ShouldBeEmpty();
}
