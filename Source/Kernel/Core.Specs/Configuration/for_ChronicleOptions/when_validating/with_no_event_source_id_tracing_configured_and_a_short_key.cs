// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class with_no_event_source_id_tracing_configured_and_a_short_key : Specification
{
    List<ValidationResult> _errors;

    void Because()
    {
        var options = new ChronicleOptions { Storage = new() { Type = StorageType.InMemory }, Telemetry = new() { EventSourceIdHashKey = "short" } };
        _errors = [.. options.Validate(new ValidationContext(options))];
    }

    [Fact] void should_reject_the_hash_key() => _errors.SelectMany(_ => _.MemberNames).ShouldContain("Telemetry.EventSourceIdHashKey");
}
