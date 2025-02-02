// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Rules;
using Cratis.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Chronicle.AspNetCore.Rules;

/// <summary>
/// Represents a <see cref="ObjectModelValidator"/> for <see cref="RulesFor{TSelf, TCommand}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RuleModelValidator"/> class.
/// </remarks>
/// <param name="ruleSets">The actual collection of <see cref="IRule">business rules</see>.</param>
/// <param name="rules">The <see cref="IRules"/>.</param>
public class RuleModelValidator(
    IEnumerable<IRule> ruleSets,
    IRules rules) : IModelValidator
{
    /// <summary>
    /// Gets the rule sets for the validator.
    /// </summary>
    public IEnumerable<IRule> RuleSets { get; } = ruleSets;

    /// <inheritdoc/>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        var failures = new List<ModelValidationResult>();
        foreach (var rule in RuleSets)
        {
            var type = context.ModelMetadata.ModelType;

            var modelIdentifier = GetModelIdentifierIfAny(context, type);

            rules.ProjectTo(rule, modelIdentifier);
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(context.ModelMetadata.ModelType);
            var validationContext = Activator.CreateInstance(validationContextType, [context.Model!]) as IValidationContext;
            var result = (rule as IValidator)!.Validate(validationContext);
            failures.AddRange(result.Errors.Select(x => new ModelValidationResult(x.PropertyName, x.ErrorMessage)));
        }

        return failures;
    }

    object? GetModelIdentifierIfAny(ModelValidationContext context, Type type)
    {
        if (type.IsRecord())
        {
            return GetModelIdentifierIfAnyFromParameters(context, type);
        }

        return GetModelIdentifierIfAnyFromProperties(context, type);
    }

    object? GetModelIdentifierIfAnyFromParameters(ModelValidationContext context, Type type)
    {
        object? modelIdentifier = null;
        var parametersWithModelKey = type
            .GetConstructors()[0]
            .GetParameters()
            .Where(_ => _.HasAttribute<ModelKeyAttribute>())
            .ToArray();

        if (parametersWithModelKey.Length > 1)
        {
            throw new InvalidNumberOfModelKeys(type, parametersWithModelKey.Where(_ => _.Name is not null).Select(_ => _.Name!));
        }

        if (parametersWithModelKey.Length == 1)
        {
            var property = type.GetProperty(parametersWithModelKey[0].Name!, BindingFlags.Instance | BindingFlags.Public);
            modelIdentifier = property!.GetValue(context.Model);
        }

        return modelIdentifier;
    }

    object? GetModelIdentifierIfAnyFromProperties(ModelValidationContext context, Type type)
    {
        object? modelIdentifier = null;
        var propertiesWithModelKey = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(_ => _.HasAttribute<ModelKeyAttribute>())
            .ToArray();

        if (propertiesWithModelKey.Length > 1)
        {
            throw new InvalidNumberOfModelKeys(type, propertiesWithModelKey.Where(_ => _.Name is not null).Select(_ => _.Name!));
        }

        if (propertiesWithModelKey.Length == 1)
        {
            modelIdentifier = propertiesWithModelKey[0].GetValue(context.Model);
        }

        return modelIdentifier;
    }
}
