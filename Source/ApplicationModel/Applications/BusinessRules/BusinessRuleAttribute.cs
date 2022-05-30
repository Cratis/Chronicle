// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a single business rule.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
public abstract class BusinessRuleAttribute : ValidationAttribute
{
    static class BusinessRulesProjectionCache<TTarget>
        where TTarget : BusinessRuleAttribute
    {
        static ProjectionDefinition? _projectionDefinition;

        public static ProjectionDefinition GetFor(BusinessRuleId identifier, IEventTypes eventTypes, IJsonSchemaGenerator jsonSchemaGenerator, BusinessRuleAttribute rule)
        {
            if (_projectionDefinition is null)
            {
                var projectionBuilder = new ProjectionBuilderFor<TTarget>(identifier.Value, eventTypes, jsonSchemaGenerator);

                var businessRuleType = typeof(TTarget);
                var defineStateMethod = businessRuleType.GetMethod("DefineState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (defineStateMethod is not null)
                {
                    var parameters = defineStateMethod.GetParameters();
                    ThrowIfInvalidSignatureForDefineState(businessRuleType, parameters);
                    defineStateMethod.Invoke(rule, new object[] { projectionBuilder });
                }

                _projectionDefinition = projectionBuilder.Build();
            }

            return _projectionDefinition;
        }

        static void ThrowIfInvalidSignatureForDefineState(Type businessRuleType, ParameterInfo[] parameters)
        {
            if (parameters.Length > 1 && parameters[0].ParameterType != typeof(IProjectionBuilderFor<>).MakeGenericType(businessRuleType))
            {
                throw new InvalidDefineStateInBusinessRuleSignature(businessRuleType);
            }
        }
    }

    /// <summary>
    /// Gets the unique identifier for the business rules.
    /// </summary>
    public abstract BusinessRuleId Identifier { get; }

    /// <summary>
    /// Validates the value it was adorned.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <returns>True if valid, false if not.</returns>
    protected new virtual bool IsValid(object? value) => true;

    /// <summary>
    /// Validates the value it was adorned.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <param name="value">Value to validate.</param>
    /// <returns>True if valid, false if not.</returns>
    protected virtual ValidationResult IsValid(ValidationContext validationContext, object? value) => ValidationResult.Success!;

    /// <inheritdoc/>
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var type = GetType();
        var cacheType = typeof(BusinessRulesProjectionCache<>).MakeGenericType(type);
        var getForMethod = cacheType.GetMethod("GetFor", BindingFlags.Static | BindingFlags.Public);

        var eventTypes = validationContext.GetService<IEventTypes>()!;
        var jsonSchemaGenerator = validationContext.GetService<IJsonSchemaGenerator>()!;
        var projectionDefinition = (getForMethod!.Invoke(null, new object[] { Identifier, eventTypes, jsonSchemaGenerator, this }) as ProjectionDefinition)!;
        var clusterClient = validationContext.GetService<IClusterClient>()!;
        var executionContext = validationContext.GetService<ExecutionContext>()!;
        var serializerOptions = validationContext.GetService<JsonSerializerOptions>()!;

        var key = new ImmediateProjectionKey(executionContext.MicroserviceId, executionContext.TenantId, Events.Store.EventSequenceId.Log, ModelKey.Unspecified);
        var projection = clusterClient.GetGrain<IImmediateProjection>(Identifier, key);
        var task = projection.GetModelInstance(projectionDefinition);
        task.Wait();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty))
        {
            var name = property.Name.ToCamelCase();
            var node = task.Result[name];
            if (node is not null)
            {
                property.SetValue(this, node.Deserialize(property.PropertyType, serializerOptions));
            }
        }

        if (!IsValid(value))
        {
            return new ValidationResult(ErrorMessage);
        }

        return IsValid(validationContext, value);
    }
}
