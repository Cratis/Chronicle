// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class with_hashed_log_identifiers_and_no_key : Specification
{
    IEnumerable<ValidationResult> _errors;

    void Because()
    {
        var options = new ChronicleOptions { Telemetry = new() { EventSourceIdsInLogs = IdentifierExposure.Hashed } };
        _errors = options.Validate(new ValidationContext(options)).ToArray();
    }

    [Fact] void should_require_a_hash_key() => _errors.SelectMany(_ => _.MemberNames).ShouldContain("Telemetry.IdentifierHashKey");
}
