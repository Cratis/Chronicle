// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Rules;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for business rules.
/// </summary>
public class RulesModelValidatorProvider : IModelValidatorProvider
{
    readonly IServiceProvider _serviceProvider;
    IRules? _rules;

    IRules Rules => _rules ??= _serviceProvider.GetService<IRules>()!;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesModelValidatorProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instance of the rules.</param>
    public RulesModelValidatorProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (Rules.HasFor(context.ModelMetadata.ModelType))
        {
            var ruleTypes = Rules.GetFor(context.ModelMetadata.ModelType);
            var rules = ruleTypes.Select(ruleType => (_serviceProvider.GetService(ruleType) as IRule)!).ToArray();

            var validator = new RuleModelValidator(rules, Rules);

            context.Results.Add(new ValidatorItem
            {
                IsReusable = false,
                Validator = validator
            });
        }
    }
}
