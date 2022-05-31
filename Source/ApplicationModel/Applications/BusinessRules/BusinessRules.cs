// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using Aksio.Cratis.Types;
using Orleans;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents an implementation of <see cref="IBusinessRules"/>.
/// </summary>
[Singleton]
public class BusinessRules : IBusinessRules
{
    static readonly MethodInfo? _createProjectionMethod;
    readonly IDictionary<Type, IEnumerable<Type>> _businessRulesPerCommand;
    readonly Dictionary<BusinessRuleId, ProjectionDefinition> _projectionDefinitionsPerRule = new();
    readonly ExecutionContext _executionContext;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly JsonSerializerOptions _serializerOptions;
    readonly IClusterClient _clusterClient;

    static BusinessRules()
    {
        _createProjectionMethod = typeof(BusinessRules).GetMethod(nameof(BusinessRules.CreateProjection), BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRules"/> class.
    /// </summary>
    /// <param name="executionContext">Current <see cref="ExecutionContext"/>.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> used for generating projection definitions.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> used for generating projection definitions.</param>
    /// <param name="serializerOptions"><see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
    public BusinessRules(
        ExecutionContext executionContext,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        JsonSerializerOptions serializerOptions,
        ITypes types,
        IClusterClient clusterClient)
    {
        var businessRuleTypes = types.All.Where(_ =>
            _.BaseType?.IsGenericType == true &&
            _.BaseType?.GetGenericTypeDefinition() == typeof(BusinessRulesFor<,>)).ToArray();

        _businessRulesPerCommand = businessRuleTypes
            .GroupBy(_ => _.BaseType!.GetGenericArguments()[1])
            .ToDictionary(_ => _.Key, _ => _.ToArray().AsEnumerable());
        _executionContext = executionContext;
        _eventTypes = eventTypes;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _serializerOptions = serializerOptions;
        _clusterClient = clusterClient;
    }

    /// <inheritdoc/>
    public bool HasFor(Type type) => _businessRulesPerCommand.ContainsKey(type);

    /// <inheritdoc/>
    public IEnumerable<Type> GetFor(Type type) => HasFor(type) ? _businessRulesPerCommand[type] : Array.Empty<Type>();

    /// <inheritdoc/>
    public ProjectionDefinition GetProjectionDefinitionFor(IBusinessRule businessRule)
    {
        if (!_projectionDefinitionsPerRule.ContainsKey(businessRule.Identifier))
        {
            _projectionDefinitionsPerRule[businessRule.Identifier] = (_createProjectionMethod!.MakeGenericMethod(businessRule.GetType()).Invoke(this, new[] { businessRule }) as ProjectionDefinition)!;
        }

        return _projectionDefinitionsPerRule[businessRule.Identifier];
    }

    /// <inheritdoc/>
    public void ProjectTo(IBusinessRule businessRule, object? modelIdentifier = default)
    {
        var projectionDefinition = GetProjectionDefinitionFor(businessRule);

        var type = businessRule.GetType();
        if (modelIdentifier is null)
        {
            var propertiesWithModelKey = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(_ => _.HasAttribute<ModelKeyAttribute>())
                .ToArray();

            if (propertiesWithModelKey.Length > 1)
            {
                throw new InvalidNumberOfModelKeys(type, propertiesWithModelKey);
            }

            if (propertiesWithModelKey.Length == 1)
            {
                modelIdentifier = propertiesWithModelKey[0].GetValue(businessRule);
            }
        }

        var key = new ImmediateProjectionKey(
            _executionContext.MicroserviceId,
            _executionContext.TenantId,
            Events.Store.EventSequenceId.Log,
            modelIdentifier is null ? ModelKey.Unspecified : modelIdentifier.ToString()!);

        var projection = _clusterClient.GetGrain<IImmediateProjection>(businessRule.Identifier.Value, key);
        var task = projection.GetModelInstance(projectionDefinition);
        task.Wait();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty))
        {
            var name = property.Name.ToCamelCase();
            var node = task.Result[name];
            if (node is not null)
            {
                property.SetValue(businessRule, node.Deserialize(property.PropertyType, _serializerOptions));
            }
        }
    }

    static void ThrowIfInvalidSignatureForDefineState(Type businessRuleType, ParameterInfo[] parameters)
    {
        if (parameters.Length > 1 && parameters[0].ParameterType != typeof(IProjectionBuilderFor<>).MakeGenericType(businessRuleType))
        {
            throw new InvalidDefineStateInBusinessRuleSignature(businessRuleType);
        }
    }

    ProjectionDefinition CreateProjection<TTarget>(IBusinessRule businessRule)
    {
        var projectionBuilder = new ProjectionBuilderFor<TTarget>(businessRule.Identifier.Value, _eventTypes, _jsonSchemaGenerator);

        var businessRuleType = typeof(TTarget);
        var defineStateMethod = businessRuleType.GetMethod("DefineState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (defineStateMethod is not null)
        {
            var parameters = defineStateMethod.GetParameters();
            ThrowIfInvalidSignatureForDefineState(businessRuleType, parameters);
            defineStateMethod.Invoke(businessRule, new object[] { projectionBuilder });
        }

        return projectionBuilder.Build();
    }
}
