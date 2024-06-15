// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Rules;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore.Rules;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for business rules.
/// </summary>
public class RulesModelValidatorProvider : IModelValidatorProvider
{
    IRules? _rules;
    IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesModelValidatorProvider"/> class.
    /// </summary>
    public RulesModelValidatorProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesModelValidatorProvider"/> class.
    /// </summary>
    /// <param name="rules"><see cref="IRules"/> to use.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances.</param>
    public RulesModelValidatorProvider(
        IRules rules,
        IServiceProvider serviceProvider)
    {
        _rules = rules;
        _serviceProvider = serviceProvider;
    }

    IRules Rules => _rules ??= GlobalInstances.ServiceProvider.GetRequiredService<IRules>();

    IServiceProvider ServiceProvider => _serviceProvider ??= GlobalInstances.ServiceProvider;

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (Rules.HasFor(context.ModelMetadata.ModelType))
        {
            var ruleTypes = Rules.GetFor(context.ModelMetadata.ModelType);
            var rules = ruleTypes.Select(ruleType => (ServiceProvider.GetRequiredService(ruleType) as IRule)!).ToArray();

            var validator = new RuleModelValidator(rules, Rules);

            context.Results.Add(new ValidatorItem
            {
                IsReusable = false,
                Validator = validator
            });
        }
    }
}
