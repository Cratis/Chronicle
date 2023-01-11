// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.Applications.Validation;

/// <summary>
/// Represents a <see cref="IModelValidator"/> for <see cref="BaseValidator{T}"/>.
/// </summary>
public class DiscoverableModelValidator : IModelValidator
{
    readonly IValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableModelValidator"/> class.
    /// </summary>
    /// <param name="validator">The <see cref="IValidator"/> to use.</param>
    public DiscoverableModelValidator(IValidator validator)
    {
        _validator = validator;
    }

    /// <inheritdoc/>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        var failures = new List<ModelValidationResult>();
        if (context.Model is not null)
        {
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(context.ModelMetadata.ModelType);
            var validationContext = Activator.CreateInstance(validationContextType, new object[] { context.Model! }) as IValidationContext;
            var result = _validator.Validate(validationContext);
            failures.AddRange(result.Errors.Select(x => new ModelValidationResult(x.PropertyName, x.ErrorMessage)));
        }
        return failures;
    }
}
