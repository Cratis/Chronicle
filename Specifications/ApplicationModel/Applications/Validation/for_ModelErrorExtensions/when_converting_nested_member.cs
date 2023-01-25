// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aksio.Cratis.Applications.Validation.for_ModelErrorExtensions;

public class when_converting_nested_member : Specification
{
    const string member = "FirstLevel.SecondLevel.TheMember";
    readonly ModelError model_error = new("Some message");
    ValidationResult validation_error;

    void Because() => validation_error = model_error.ToValidationResult(member);

    [Fact] void should_hold_message() => validation_error.Message.ShouldEqual(model_error.ErrorMessage);
    [Fact] void should_hold_camel_cased_member() => validation_error.Members.First().ShouldEqual("firstLevel.secondLevel.theMember");
}
