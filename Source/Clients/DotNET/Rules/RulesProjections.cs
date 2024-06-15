// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.DependencyInjection;
using Cratis.Events;
using Cratis.Models;
using Cratis.Projections;
using Cratis.Schemas;

namespace Cratis.Rules;

/// <summary>
/// Represents an implementation of <see cref="IRulesProjections"/>.
/// </summary>
[Singleton]
public class RulesProjections : IRulesProjections
{
    readonly IEventTypes _eventTypes;
    readonly IModelNameResolver _modelNameResolver;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly JsonSerializerOptions _serializerOptions;
    readonly Dictionary<RuleId, ProjectionDefinition> _projectionDefinitionsPerRuleId;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesProjections"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> used for generating projection definitions.</param>
    /// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> used for generating projection definitions.</param>
    /// <param name="serializerOptions"><see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    public RulesProjections(
        IServiceProvider serviceProvider,
        IClientArtifactsProvider clientArtifacts,
        IEventTypes eventTypes,
        IModelNameResolver modelNameResolver,
        IJsonSchemaGenerator jsonSchemaGenerator,
        JsonSerializerOptions serializerOptions)
    {
        _eventTypes = eventTypes;
        _modelNameResolver = modelNameResolver;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _serializerOptions = serializerOptions;

        var createProjectionMethod = typeof(RulesProjections).GetMethod(nameof(CreateProjection), BindingFlags.NonPublic | BindingFlags.Instance)!;
        _projectionDefinitionsPerRuleId = clientArtifacts.Rules.Select(ruleType =>
        {
            var rule = serviceProvider.GetService(ruleType);
            return (createProjectionMethod!.MakeGenericMethod(ruleType).Invoke(this, [rule]) as ProjectionDefinition)!;
        }).ToDictionary(_ => (RuleId)_.Identifier, _ => _);

        Definitions = _projectionDefinitionsPerRuleId.Values.ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; }

    /// <inheritdoc/>
    public bool HasFor(RuleId ruleId) => _projectionDefinitionsPerRuleId.ContainsKey(ruleId);

    /// <inheritdoc/>
    public ProjectionDefinition GetFor(RuleId ruleId)
    {
        ThrowIfMissingProjectionForRule(ruleId);

        return _projectionDefinitionsPerRuleId[ruleId];
    }

    void ThrowIfMissingProjectionForRule(RuleId ruleId)
    {
        if (!HasFor(ruleId)) throw new MissingProjectionForRule(ruleId);
    }

    ProjectionDefinition CreateProjection<TTarget>(IRule rule)
    {
        var projectionBuilder = new ProjectionBuilderFor<TTarget>(rule.Identifier.Value, _modelNameResolver, _eventTypes, _jsonSchemaGenerator, _serializerOptions);

        var ruleType = typeof(TTarget);

        var defineStateMethod = ruleType.GetMethod("DefineState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (defineStateMethod is not null)
        {
            var parameters = defineStateMethod.GetParameters();
            ThrowIfInvalidSignatureForDefineState(ruleType, parameters);
            defineStateMethod.Invoke(rule, new object[] { projectionBuilder });
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
