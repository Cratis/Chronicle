// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using Aksio.Cratis.Types;
using Orleans;

namespace Aksio.Cratis.Applications.Rules;

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
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly JsonSerializerOptions _serializerOptions;
    readonly IClusterClient _clusterClient;

    static Rules()
    {
        _createProjectionMethod = typeof(Rules).GetMethod(nameof(Rules.CreateProjection), BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rules"/> class.
    /// </summary>
    /// <param name="executionContext">Current <see cref="ExecutionContext"/>.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> used for generating projection definitions.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> used for generating projection definitions.</param>
    /// <param name="serializerOptions"><see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
    public Rules(
        ExecutionContext executionContext,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        JsonSerializerOptions serializerOptions,
        ITypes types,
        IClusterClient clusterClient)
    {
        var ruleTypes = types.All.Where(_ =>
            _.BaseType?.IsGenericType == true &&
            _.BaseType?.GetGenericTypeDefinition() == typeof(RulesFor<,>)).ToArray();

        _rulesPerCommand = ruleTypes
            .GroupBy(_ => _.BaseType!.GetGenericArguments()[1])
            .ToDictionary(_ => _.Key, _ => _.ToArray().AsEnumerable());
        _executionContext = executionContext;
        _eventTypes = eventTypes;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _serializerOptions = serializerOptions;
        _clusterClient = clusterClient;
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

        var key = new ImmediateProjectionKey(
            _executionContext.MicroserviceId,
            _executionContext.TenantId,
            Events.Store.EventSequenceId.Log,
            modelIdentifier is null ? ModelKey.Unspecified : modelIdentifier.ToString()!);

        var projection = _clusterClient.GetGrain<IImmediateProjection>(rule.Identifier.Value, key);
        var result = projection.GetModelInstance(projectionDefinition).GetAwaiter().GetResult();

        foreach (var property in rule.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty))
        {
            var name = property.Name.ToCamelCase();
            var node = result[name];
            if (node is not null)
            {
                property.SetValue(rule, node.Deserialize(property.PropertyType, _serializerOptions));
            }
        }
    }

    ProjectionDefinition CreateProjection<TTarget>(IRule rule)
    {
        var projectionBuilder = new ProjectionBuilderFor<TTarget>(rule.Identifier.Value, _eventTypes, _jsonSchemaGenerator);

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
