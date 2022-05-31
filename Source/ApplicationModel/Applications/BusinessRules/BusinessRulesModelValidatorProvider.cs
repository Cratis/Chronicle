// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for business rules.
/// </summary>
public class BusinessRulesModelValidatorProvider : IModelValidatorProvider
{
    readonly IServiceProvider _serviceProvider;
    IBusinessRules? _businessRules;

    IBusinessRules BusinessRules => _businessRules ??= _serviceProvider.GetService<IBusinessRules>()!;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRulesModelValidatorProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instance of the rules.</param>
    public BusinessRulesModelValidatorProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (BusinessRules.HasFor(context.ModelMetadata.ModelType))
        {
            var ruleTypes = BusinessRules.GetFor(context.ModelMetadata.ModelType);
            var rules = ruleTypes.Select(ruleType => (_serviceProvider.GetService(ruleType) as IBusinessRule)!).ToArray();

            var validator = new BusinessRuleModelValidator(rules, BusinessRules);

            context.Results.Add(new ValidatorItem
            {
                IsReusable = false,
                Validator = validator
            });
        }
    }
}
