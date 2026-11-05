// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Reads the property mappings the model-bound attributes on the members of a node or edge declare per event type.
/// </summary>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal class GraphMemberMappings(INamingPolicy namingPolicy)
{
    /// <summary>
    /// Read the explicit property expressions.
    /// </summary>
    /// <param name="members">The members to read.</param>
    /// <returns>For each event type, the property name to expression.</returns>
    public IReadOnlyDictionary<Type, Dictionary<string, string>> Read(IEnumerable<GraphMember> members)
    {
        var result = new Dictionary<Type, Dictionary<string, string>>();
        foreach (var member in members)
        {
            var property = namingPolicy.GetPropertyName(member.Name);
            foreach (var attribute in member.Attributes.OfType<IEventBoundAttribute>())
            {
                if (ExpressionFor(attribute, member.Name) is not { } expression) continue;

                if (!result.TryGetValue(attribute.EventType, out var properties)) result[attribute.EventType] = properties = [];
                properties[property] = expression;
            }
        }

        return result;
    }

    static string? ValueExpression(object? value) => value is null ? WellKnownExpressions.Null : $"{WellKnownExpressions.Value}({Invariant(value)})";

    static string Invariant(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
        Enum enumeration => enumeration.ToString(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };

    string Source(string name) => (string)ProjectionPropertyPaths.Resolve(name, namingPolicy, ProjectionPathRole.Source);

    string? ExpressionFor(IEventBoundAttribute attribute, string memberName) => attribute switch
    {
        ISetFromAttribute set => Source(set.EventPropertyName ?? memberName),
        ISetFromContextAttribute context => $"{WellKnownExpressions.EventContext}({ProjectionPropertyPaths.RenderContextPath(context.ContextPropertyName ?? memberName, namingPolicy)})",
        IAddFromAttribute add => $"{WellKnownExpressions.Add}({Source(add.EventPropertyName ?? memberName)})",
        ISubtractFromAttribute subtract => $"{WellKnownExpressions.Subtract}({Source(subtract.EventPropertyName ?? memberName)})",
        IIncrementAttribute => WellKnownExpressions.Increment,
        IDecrementAttribute => WellKnownExpressions.Decrement,
        ICountAttribute => WellKnownExpressions.Count,
        ISetValueAttribute setValue => ValueExpression(setValue.Value),
        _ => null
    };
}
