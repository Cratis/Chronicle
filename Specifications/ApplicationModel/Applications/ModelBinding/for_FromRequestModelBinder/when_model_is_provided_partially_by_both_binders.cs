// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aksio.Cratis.Applications.ModelBinding.for_FromRequestModelBinder;

public record TheModel(int intValue, string stringValue, int secondIntValue, string secondStringValue);

public class when_model_is_provided_partially_by_both_binders : Specification
{
    FromRequestModelBinder binder;
    Mock<IModelBinder> body_binder;
    Mock<IModelBinder> complex_binder;

    Mock<ModelBindingContext> context;
    ModelBindingResult result;

    ModelBindingResult final_result;

    TheModel body_model;
    TheModel complex_model;

    void Establish()
    {
        body_model = new TheModel(42, "forty two", 0, null!);
        complex_model = new TheModel(0, null!, 43, "forty three");

        context = new();
        context.SetupGet(_ => _.Result).Returns(() => result);
        context.SetupSet(_ => _.Result = IsAny<ModelBindingResult>()).Callback<ModelBindingResult>(r => final_result = r);

        body_binder = new();
        body_binder.Setup(_ => _.BindModelAsync(context.Object)).Returns((ModelBindingContext _) =>
        {
            result = ModelBindingResult.Success(body_model);
            return Task.CompletedTask;
        });

        complex_binder = new();
        complex_binder.Setup(_ => _.BindModelAsync(context.Object)).Returns((ModelBindingContext _) =>
        {
            result = ModelBindingResult.Success(complex_model);
            return Task.CompletedTask;
        });

        binder = new(body_binder.Object, complex_binder.Object);
    }

    Task Because() => binder.BindModelAsync(context.Object);

    [Fact] void should_hold_body_model_int() => ((TheModel)final_result.Model).intValue.ShouldEqual(body_model.intValue);
    [Fact] void should_hold_body_model_string() => ((TheModel)final_result.Model).stringValue.ShouldEqual(body_model.stringValue);
    [Fact] void should_hold_complex_model_int() => ((TheModel)final_result.Model).secondIntValue.ShouldEqual(complex_model.secondIntValue);
    [Fact] void should_hold_complex_model_string() => ((TheModel)final_result.Model).secondStringValue.ShouldEqual(complex_model.secondStringValue);
}
