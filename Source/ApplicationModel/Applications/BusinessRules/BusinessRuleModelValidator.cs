// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Strings;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Orleans;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a <see cref="ObjectModelValidator"/> for <see cref="BusinessRulesFor{TSelf, TCommand}"/>.
/// </summary>
public class BusinessRuleModelValidator : IModelValidator
{
    readonly ExecutionContext _executionContext;
    readonly IEnumerable<BusinessRuleValidatorAndProjectionDefinition> _validatorsAndProjectionDefinitions;
    readonly JsonSerializerOptions _serializerOptions;
    readonly IClusterClient _clusterClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleModelValidator"/> class.
    /// </summary>
    /// <param name="executionContext">Current <see cref="ExecutionContext"/>.</param>
    /// <param name="validatorsAndProjectionDefinitions">Validators and belonging projection definitions.</param>
    /// <param name="serializerOptions"><see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
    public BusinessRuleModelValidator(
        ExecutionContext executionContext,
        IEnumerable<BusinessRuleValidatorAndProjectionDefinition> validatorsAndProjectionDefinitions,
        JsonSerializerOptions serializerOptions,
        IClusterClient clusterClient)
    {
        _executionContext = executionContext;
        _validatorsAndProjectionDefinitions = validatorsAndProjectionDefinitions;
        _serializerOptions = serializerOptions;
        _clusterClient = clusterClient;
    }

    /// <inheritdoc/>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        var key = new ImmediateProjectionKey(_executionContext.MicroserviceId, _executionContext.TenantId, Events.Store.EventSequenceId.Log, ModelKey.Unspecified);

        foreach (var validatorAndProjection in _validatorsAndProjectionDefinitions)
        {
            var projection = _clusterClient.GetGrain<IImmediateProjection>(validatorAndProjection.Identifier, key);
            var task = projection.GetModelInstance(validatorAndProjection.ProjectionDefinition);
            task.Wait();

            foreach (var property in validatorAndProjection.Validator.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var name = property.Name.ToCamelCase();
                var node = task.Result[name];
                if (node is not null)
                {
                    property.SetValue(validatorAndProjection.Validator, node.Deserialize(property.PropertyType, _serializerOptions));
                }
            }

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(context.ModelMetadata.ModelType);
            var validationContext = Activator.CreateInstance(validationContextType, new object[] { context.Model! }) as IValidationContext;
            var result = validatorAndProjection.Validator.Validate(validationContext);
            return result.Errors.Select(x => new ModelValidationResult(x.PropertyName, x.ErrorMessage));
        }

        return Array.Empty<ModelValidationResult>();
    }
}
