// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Schemas;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for business rules.
/// </summary>
public class BusinessRulesModelValidatorProvider : IModelValidatorProvider
{
    static class BusinessRulesProjectionCache<TSelf, TCommand>
        where TSelf : BusinessRulesFor<TSelf, TCommand>
    {
        static ProjectionDefinition? _projectionDefinition;

        public static ProjectionDefinition GetFor(IEventTypes eventTypes, IJsonSchemaGenerator jsonSchemaGenerator, object rules)
        {
            if (_projectionDefinition is null)
            {
                var actualRules = (rules as BusinessRulesFor<TSelf, TCommand>)!;
                var projectionBuilder = new ProjectionBuilderFor<TSelf>(actualRules.Identifier.Value, eventTypes, jsonSchemaGenerator);
                actualRules.DefineState(projectionBuilder);
                _projectionDefinition = projectionBuilder.Build();
            }

            return _projectionDefinition;
        }
    }

    readonly IBusinessRules _businessRules;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRulesModelValidatorProvider"/> class.
    /// </summary>
    /// <param name="businessRules"><see cref="IBusinessRules"/> for getting rules for types.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instance of the rules.</param>
    public BusinessRulesModelValidatorProvider(
        IBusinessRules businessRules,
        IServiceProvider serviceProvider)
    {
        _businessRules = businessRules;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (_businessRules.HasFor(context.ModelMetadata.ModelType))
        {
            var ruleTypes = _businessRules.GetFor(context.ModelMetadata.ModelType);
            var rules = ruleTypes.Select(ruleType =>
            {
                var businessRules = (_serviceProvider.GetService(ruleType) as IValidator)!;
                var cacheType = typeof(BusinessRulesProjectionCache<,>).MakeGenericType(ruleType.BaseType!.GenericTypeArguments);
                var getForMethod = cacheType.GetMethod("GetFor", BindingFlags.Static | BindingFlags.Public);

                var eventTypes = _serviceProvider.GetService<IEventTypes>()!;
                var jsonSchemaGenerator = _serviceProvider.GetService<IJsonSchemaGenerator>()!;
                var projectionDefinition = (getForMethod!.Invoke(null, new object[] { eventTypes, jsonSchemaGenerator, businessRules }) as ProjectionDefinition)!;

                return new BusinessRuleValidatorAndProjectionDefinition(projectionDefinition.Identifier.Value, businessRules, projectionDefinition);
            }).ToArray();
            Console.WriteLine(rules);

            var validator = new BusinessRuleModelValidator(
                _serviceProvider.GetService<ExecutionContext>()!,
                rules,
                _serviceProvider.GetService<JsonSerializerOptions>()!,
                _serviceProvider.GetService<IClusterClient>()!);

            context.Results.Add(new ValidatorItem
            {
                IsReusable = false,
                Validator = validator
            });
        }
    }
}
