// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Represents a <see cref="ObjectModelValidator"/> for <see cref="RulesFor{TSelf, TCommand}"/>.
/// </summary>
public class RuleModelValidator : IModelValidator
{
    readonly IEnumerable<IRule> _ruleSets;
    readonly IRules _rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleModelValidator"/> class.
    /// </summary>
    /// <param name="ruleSets">The actual collection of <see cref="IRule">business rules</see>.</param>
    /// <param name="rules">The <see cref="IRules"/>.</param>
    public RuleModelValidator(
        IEnumerable<IRule> ruleSets,
        IRules rules)
    {
        _ruleSets = ruleSets;
        _rules = rules;
    }

    /// <inheritdoc/>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        foreach (var businessRule in _ruleSets)
        {
            _rules.ProjectTo(businessRule);
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(context.ModelMetadata.ModelType);
            var validationContext = Activator.CreateInstance(validationContextType, new object[] { context.Model! }) as IValidationContext;
            var result = (businessRule as IValidator)!.Validate(validationContext);
            return result.Errors.Select(x => new ModelValidationResult(x.PropertyName, x.ErrorMessage));
        }

        return Array.Empty<ModelValidationResult>();
    }
}
