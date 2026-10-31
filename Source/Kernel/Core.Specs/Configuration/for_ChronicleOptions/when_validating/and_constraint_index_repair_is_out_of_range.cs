// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class and_constraint_index_repair_is_out_of_range : Specification
{
    ChronicleOptions _options;
    IEnumerable<ValidationResult> _errors;

    void Establish() => _options = new() { ConstraintIndexRepair = new() { CatchUpSlots = 0 } };

    void Because() => _errors = _options.Validate(new ValidationContext(_options)).ToArray();

    [Fact] void should_reject_the_configuration() => _errors.ShouldContain(error => error.MemberNames.Contains("ConstraintIndexRepair.CatchUpSlots"));
}
