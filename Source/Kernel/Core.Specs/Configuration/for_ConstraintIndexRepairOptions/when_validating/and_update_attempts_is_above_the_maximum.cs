// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_ConstraintIndexRepairOptions.when_validating;

public class and_update_attempts_is_above_the_maximum : Specification
{
    ValidationResult[] _errors;

    void Because() => _errors = new ConstraintIndexRepairOptions { UpdateAttempts = 11 }.Validate().ToArray();

    [Fact] void should_refuse_it() => _errors.Length.ShouldEqual(1);
    [Fact] void should_name_the_setting_in_the_message() => _errors[0].ErrorMessage.ShouldContain("ConstraintIndexRepair.UpdateAttempts");
}
