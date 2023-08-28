// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Projections;
using Aksio.Strings;

namespace Aksio.Cratis.Rules;

/// <summary>
/// Represents an implementation of <see cref="IRules"/>.
/// </summary>
[Singleton]
public class Rules : IRules
{
    readonly IDictionary<Type, IEnumerable<Type>> _rulesPerCommand;
    readonly JsonSerializerOptions _serializerOptions;
    readonly IRulesProjections _rulesProjections;
    readonly IImmediateProjections _immediateProjections;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rules"/> class.
    /// </summary>
    /// <param name="serializerOptions"><see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    /// <param name="rulesProjections">All <see cref="IRulesProjections"/>.</param>
    /// <param name="immediateProjections"><see cref="IImmediateProjections"/> client.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    public Rules(
        JsonSerializerOptions serializerOptions,
        IRulesProjections rulesProjections,
        IImmediateProjections immediateProjections,
        IClientArtifactsProvider clientArtifacts)
    {
        _rulesPerCommand = clientArtifacts.Rules
            .GroupBy(_ => _.BaseType!.GetGenericArguments()[1])
            .ToDictionary(_ => _.Key, _ => _.ToArray().AsEnumerable());
        _serializerOptions = serializerOptions;
        _rulesProjections = rulesProjections;
        _immediateProjections = immediateProjections;
    }

    /// <inheritdoc/>
    public bool HasFor(Type type) => _rulesPerCommand.ContainsKey(type);

    /// <inheritdoc/>
    public IEnumerable<Type> GetFor(Type type) => HasFor(type) ? _rulesPerCommand[type] : Array.Empty<Type>();

    /// <inheritdoc/>
    public void ProjectTo(IRule rule, object? modelIdentifier = default)
    {
        if (!_rulesProjections.HasFor(rule.Identifier)) return;

        var result = _immediateProjections.GetInstanceById(
            rule.Identifier.Value,
            modelIdentifier is null ? ModelKey.Unspecified : modelIdentifier.ToString()!).GetAwaiter().GetResult();

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
}
