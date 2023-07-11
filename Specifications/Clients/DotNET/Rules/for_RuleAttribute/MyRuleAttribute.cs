// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Aksio.Cratis.Rules.for_Rules.for_RuleAttribute;

public class MyRuleAttribute : RuleAttribute
{
    public const string error_message = "The Error Message";
    public override RuleId Identifier => "8ee57f0d-07b8-4255-a163-ef43aff0a52d";

    public bool simple_is_valid_return = true;
    public bool simple_is_valid_called;
    public object value_validated;

    public ValidationResult complex_is_value_return = ValidationResult.Success;
    public bool complex_is_valid_called;

    public override string FormatErrorMessage(string name) => error_message;

    protected override bool IsValid(object value)
    {
        value_validated = value;
        simple_is_valid_called = true;
        return simple_is_valid_return;
    }

    protected override ValidationResult IsValid(ValidationContext validationContext, object value)
    {
        value_validated = value;
        complex_is_valid_called = true;
        return complex_is_value_return;
    }
}
