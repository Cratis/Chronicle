// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a <see cref="ObjectModelValidator"/> for <see cref="BusinessRulesFor{TSelf, TCommand}"/>.
/// </summary>
public class BusinessRuleModelValidator : IModelValidator
{
    readonly IEnumerable<IBusinessRule> _businessRuleSets;
    readonly IBusinessRules _businessRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleModelValidator"/> class.
    /// </summary>
    /// <param name="businessRuleSets">The actual collection of <see cref="IBusinessRule">business rules</see>.</param>
    /// <param name="businessRules">The <see cref="IBusinessRules"/>.</param>
    public BusinessRuleModelValidator(
        IEnumerable<IBusinessRule> businessRuleSets,
        IBusinessRules businessRules)
    {
        _businessRuleSets = businessRuleSets;
        _businessRules = businessRules;
    }

    /// <inheritdoc/>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        foreach (var businessRule in _businessRuleSets)
        {
            _businessRules.ProjectTo(businessRule);
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(context.ModelMetadata.ModelType);
            var validationContext = Activator.CreateInstance(validationContextType, new object[] { context.Model! }) as IValidationContext;
            var result = (businessRule as IValidator)!.Validate(validationContext);
            return result.Errors.Select(x => new ModelValidationResult(x.PropertyName, x.ErrorMessage));
        }

        return Array.Empty<ModelValidationResult>();
    }
}
