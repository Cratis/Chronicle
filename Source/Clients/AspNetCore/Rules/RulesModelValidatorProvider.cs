// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Rules;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.AspNetCore.Rules;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for business rules.
/// </summary>
public class RulesModelValidatorProvider : IModelValidatorProvider
{
    IRules? _rules;

    IRules Rules => _rules ??= GlobalInstances.ServiceProvider.GetRequiredService<IRules>();

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (Rules.HasFor(context.ModelMetadata.ModelType))
        {
            var ruleTypes = Rules.GetFor(context.ModelMetadata.ModelType);
            var rules = ruleTypes.Select(ruleType => (GlobalInstances.ServiceProvider.GetService(ruleType) as IRule)!).ToArray();

            var validator = new RuleModelValidator(rules, Rules);

            context.Results.Add(new ValidatorItem
            {
                IsReusable = false,
                Validator = validator
            });
        }
    }
}
