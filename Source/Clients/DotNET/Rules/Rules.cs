// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis;
using Aksio.Cratis.Events;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Models;
using Aksio.Schemas;
using Aksio.Strings;

namespace Aksio.Rules;

/// <summary>
/// Represents an implementation of <see cref="IRules"/>.
/// </summary>
[Singleton]
public class Rules : IRules
{
    static readonly MethodInfo? _createProjectionMethod;
    readonly IDictionary<Type, IEnumerable<Type>> _rulesPerCommand;
    readonly Dictionary<RuleId, ProjectionDefinition> _projectionDefinitionsPerRule = new();
    readonly ExecutionContext _executionContext;
    readonly IModelNameConvention _modelNameConvention;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly JsonSerializerOptions _serializerOptions;
    readonly IImmediateProjections _immediateProjections;

    static Rules()
    {
        _createProjectionMethod = typeof(Rules).GetMethod(nameof(Rules.CreateProjection), BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rules"/> class.
    /// </summary>
    /// <param name="executionContext">Current <see cref="ExecutionContext"/>.</param>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> used for generating projection definitions.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> used for generating projection definitions.</param>
    /// <param name="serializerOptions"><see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    /// <param name="immediateProjections"><see cref="IImmediateProjections"/> client.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    public Rules(
        ExecutionContext executionContext,
        IModelNameConvention modelNameConvention,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        JsonSerializerOptions serializerOptions,
        IImmediateProjections immediateProjections,
        IClientArtifactsProvider clientArtifacts)
    {
        _rulesPerCommand = clientArtifacts.Rules
            .GroupBy(_ => _.BaseType!.GetGenericArguments()[1])
            .ToDictionary(_ => _.Key, _ => _.ToArray().AsEnumerable());
        _executionContext = executionContext;
        _modelNameConvention = modelNameConvention;
        _eventTypes = eventTypes;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _serializerOptions = serializerOptions;
        _immediateProjections = immediateProjections;
    }

    /// <inheritdoc/>
    public bool HasFor(Type type) => _rulesPerCommand.ContainsKey(type);

    /// <inheritdoc/>
    public IEnumerable<Type> GetFor(Type type) => HasFor(type) ? _rulesPerCommand[type] : Array.Empty<Type>();

    /// <inheritdoc/>
    public ProjectionDefinition GetProjectionDefinitionFor(IRule rule)
    {
        if (!_projectionDefinitionsPerRule.ContainsKey(rule.Identifier))
        {
            try
            {
                _projectionDefinitionsPerRule[rule.Identifier] = (_createProjectionMethod!.MakeGenericMethod(rule.GetType()).Invoke(this, new[] { rule }) as ProjectionDefinition)!;
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is not null)
                {
                    throw ex.InnerException;
                }

                throw;
            }
        }

        return _projectionDefinitionsPerRule[rule.Identifier];
    }

    /// <inheritdoc/>
    public void ProjectTo(IRule rule, object? modelIdentifier = default)
    {
        var projectionDefinition = GetProjectionDefinitionFor(rule);
        if (projectionDefinition.IsEmpty)
        {
            return;
        }

        var result = _immediateProjections.GetInstanceById(
            modelIdentifier is null ? ModelKey.Unspecified : modelIdentifier.ToString()!,
            projectionDefinition).GetAwaiter().GetResult();

        foreach (var property in rule.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty))
        {
            var name = property.Name.ToCamelCase();
            var node = result.Model[name];
            if (node is not null)
            {
                property.SetValue(rule, node.Deserialize(property.PropertyType, _serializerOptions));
            }
        }
    }

    ProjectionDefinition CreateProjection<TTarget>(IRule rule)
    {
        var projectionBuilder = new ProjectionBuilderFor<TTarget>(rule.Identifier.Value, _modelNameConvention, _eventTypes, _jsonSchemaGenerator, _serializerOptions);

        var ruleType = typeof(TTarget);
        var defineStateMethod = ruleType.GetMethod("DefineState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (defineStateMethod is not null)
        {
            var parameters = defineStateMethod.GetParameters();
            ThrowIfInvalidSignatureForDefineState(ruleType, parameters);
            defineStateMethod.Invoke(rule, new object[] { projectionBuilder });
        }

        return projectionBuilder.Build();
    }

    void ThrowIfInvalidSignatureForDefineState(Type ruleType, ParameterInfo[] parameters)
    {
        if (parameters.Length > 1 && parameters[0].ParameterType != typeof(IProjectionBuilderFor<>).MakeGenericType(ruleType))
        {
            throw new InvalidDefineStateInRuleSignature(ruleType);
        }
    }
}
