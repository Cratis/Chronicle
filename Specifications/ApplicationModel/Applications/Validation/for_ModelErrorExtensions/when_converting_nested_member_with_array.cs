// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aksio.Cratis.Applications.Validation.for_ModelErrorExtensions;

public class when_converting_nested_member_with_array : Specification
{
    const string member = "FirstLevel[0].SecondLevel[1].TheMember";
    readonly ModelError model_error = new ModelError("Some message");
    ValidationError validation_error;

    void Because() => validation_error = model_error.ToValidationError(member);

    [Fact] void should_hold_message() => validation_error.Message.ShouldEqual(model_error.ErrorMessage);

    [Fact] void should_hold_camel_cased_member() => validation_error.MemberNames.First().ShouldEqual("firstLevel[0].secondLevel[1].theMember");
}
