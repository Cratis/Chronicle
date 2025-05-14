// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Models;

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Represents an implementation of <see cref="IRulesProjections"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RulesProjections"/> class.
/// </remarks>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances.</param>
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> used for generating projection definitions.</param>
/// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
/// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> used for generating projection definitions.</param>
/// <param name="serializerOptions"><see cref="JsonSerializerOptions"/> to use for deserialization.</param>
[Singleton]
internal class RulesProjections(
    IServiceProvider serviceProvider,
    IClientArtifactsProvider clientArtifacts,
    IEventTypes eventTypes,
    IModelNameResolver modelNameResolver,
    IJsonSchemaGenerator jsonSchemaGenerator,
    JsonSerializerOptions serializerOptions) : IRulesProjections
{
    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Discover()
    {
        var createProjectionMethod = typeof(RulesProjections).GetMethod(nameof(CreateProjection), BindingFlags.NonPublic | BindingFlags.Instance)!;
        return clientArtifacts.Rules.Select(ruleType =>
        {
            var rule = serviceProvider.GetService(ruleType);
            return (createProjectionMethod!.MakeGenericMethod(ruleType).Invoke(this, [rule]) as ProjectionDefinition)!;
        }).ToImmutableList();
    }

    ProjectionDefinition CreateProjection<TTarget>(IRule rule)
    {
        var identifier = rule.GetType().GetRuleId();
        var projectionBuilder = new ProjectionBuilderFor<TTarget>(identifier.Value, modelNameResolver, eventTypes, jsonSchemaGenerator, serializerOptions);

        var ruleType = typeof(TTarget);

        var defineStateMethod = ruleType.GetMethod("DefineState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (defineStateMethod is not null)
        {
            var parameters = defineStateMethod.GetParameters();
            ThrowIfInvalidSignatureForDefineState(ruleType, parameters);
            defineStateMethod.Invoke(rule, [projectionBuilder]);
        }

        var definition = projectionBuilder.Build();
        definition.IsActive = false;
        return definition;
    }

    void ThrowIfInvalidSignatureForDefineState(Type ruleType, ParameterInfo[] parameters)
    {
        if (parameters.Length > 1 && parameters[0].ParameterType != typeof(IProjectionBuilderFor<>).MakeGenericType(ruleType))
        {
            throw new InvalidDefineStateInRuleSignature(ruleType);
        }
    }
}
