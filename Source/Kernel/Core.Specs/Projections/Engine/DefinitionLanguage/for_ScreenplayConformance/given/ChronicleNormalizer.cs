// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// Normalizes a <see cref="ProjectionDefinition"/> produced by Chronicle's lowering into the neutral tree.
/// </summary>
/// <remarks>
/// Equivalence rules applied here, so they are not reported as differences:
/// <list type="bullet">
/// <item>A missing key (<see cref="PropertyExpression.NotSet"/>) is the event source id, for transitions, removals and join removals.</item>
/// <item>A missing parent key inside a child collection (at any depth below it) is the event source id; at a projection's own
/// level, or in a nested object that is not inside a child collection, there is no parent.</item>
/// <item><c language="csharp">$count</c> is <c language="csharp">$increment</c>, and <c language="csharp">$null</c> is a clear.</item>
/// <item><c language="csharp">$eventContext(eventSourceId)</c> is the event source id.</item>
/// <item>An empty <c language="csharp">every</c> that neither includes children nor subscribes to all events is no <c language="csharp">every</c>.</item>
/// <item>AutoMap is expanded with the engine's own merge (<see cref="ProjectionFactory"/>) against the case's schemas, because the
/// definition keeps AutoMap as a flag that the engine resolves at runtime while the semantic model expands it at bind time.</item>
/// </list>
/// </remarks>
public static class ChronicleNormalizer
{
    static readonly MethodInfo _mergeFromProperties = typeof(ProjectionFactory).GetMethod("GetMergedFromProperties", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(ProjectionFactory), "GetMergedFromProperties");

    /// <summary>
    /// Normalizes a projection definition.
    /// </summary>
    /// <param name="definition">The definition to normalize.</param>
    /// <param name="schemas">The case's schemas.</param>
    /// <returns>The neutral projection level.</returns>
    public static NeutralScope Normalize(ProjectionDefinition definition, ConformanceSchemas schemas) =>
        Scope(definition, schemas, schemas.Root, isChildContext: false, isRoot: true);

    static NeutralScope Scope(ProjectionDefinition definition, ConformanceSchemas schemas, ConformanceLevel? level, bool isChildContext, bool isRoot)
    {
        var autoMap = definition.AutoMap;
        var from = definition.From.Select(_ => new NeutralFrom(
            _.Key.Id.Value,
            Key(_.Value.Key),
            ParentKey(_.Value.ParentKey, isChildContext),
            Mappings(MergeFrom(_.Value, level, schemas.Event(_.Key.Id.Value), autoMap)))).ToArray();
        var joins = definition.Join.Select(_ => new NeutralJoin(
            _.Key.Id.Value,
            _.Value.On.Path,
            _.Value.Key.IsSet() ? Key(_.Value.Key) : null,
            Mappings(MergeJoin(_.Value, level, schemas.Event(_.Key.Id.Value), autoMap)))).ToArray();
        var children = definition.Children.Select(_ => new NeutralChildren(
            _.Key.Path,
            _.Value.IdentifiedBy.Path,
            Scope(_.Value, schemas, level?.Child(_.Key.Path), isChildContext: true, isRoot: false))).ToArray();
        var nested = (definition.Nested ?? new Dictionary<PropertyPath, ChildrenDefinition>()).Select(_ => new NeutralNested(
            _.Key.Path,
            Scope(_.Value, schemas, level?.Child(_.Key.Path), isChildContext, isRoot: false))).ToArray();
        var removals = definition.RemovedWith.Select(_ => new NeutralRemoval(
            _.Key.Id.Value,
            Key(_.Value.Key),
            ParentKey(_.Value.ParentKey, isChildContext))).ToArray();
        var joinRemovals = definition.RemovedWithJoin.Select(_ => new NeutralJoinRemoval(_.Key.Id.Value, Key(_.Value.Key))).ToArray();

        var subscribesToAllEvents = isRoot && definition.SubscribesToAllEvents;
        var every = definition.FromEvery;
        var everyMappings = Mappings(every.Properties);
        var neutralEvery = everyMappings.Count == 0 && !every.IncludeChildren && !subscribesToAllEvents
            ? null
            : new NeutralEvery(every.IncludeChildren, subscribesToAllEvents, everyMappings);

        return new(from, joins, children, nested, neutralEvery, removals, joinRemovals);
    }

    static IEnumerable<KeyValuePair<PropertyPath, string>> MergeFrom(FromDefinition from, ConformanceLevel? level, JsonSchema? eventSchema, AutoMap autoMap) =>
        level is null
            ? from.Properties
            : (List<KeyValuePair<PropertyPath, string>>)_mergeFromProperties.Invoke(null, [from, level.Schema, eventSchema, autoMap, new HashSet<string>()])!;

    static IEnumerable<KeyValuePair<PropertyPath, string>> MergeJoin(JoinDefinition join, ConformanceLevel? level, JsonSchema? eventSchema, AutoMap autoMap) =>
        level is null
            ? join.Properties
            : ProjectionFactory.GetMergedJoinProperties(join, level.Schema, eventSchema, autoMap, new HashSet<string>());

    static List<NeutralMapping> Mappings(IEnumerable<KeyValuePair<PropertyPath, string>> properties) =>
        properties.Select(_ => Mapping(_.Key.Path, _.Value)).ToList();

    static NeutralMapping Mapping(string target, string expression)
    {
        if (expression == WellKnownExpressions.Null)
        {
            return new(target, NeutralOperation.Clear, null);
        }

        if (string.Equals(expression, WellKnownExpressions.Increment, StringComparison.Ordinal) || string.Equals(expression, WellKnownExpressions.Count, StringComparison.Ordinal))
        {
            return new(target, NeutralOperation.Increment, null);
        }

        if (expression == WellKnownExpressions.Decrement)
        {
            return new(target, NeutralOperation.Decrement, null);
        }

        if (TryUnwrap(expression, WellKnownExpressions.Add, out var added))
        {
            return new(target, NeutralOperation.Add, Value(added));
        }

        if (TryUnwrap(expression, WellKnownExpressions.Subtract, out var subtracted))
        {
            return new(target, NeutralOperation.Subtract, Value(subtracted));
        }

        return new(target, NeutralOperation.Set, Value(expression));
    }

    static NeutralKey Key(PropertyExpression expression)
    {
        if (!expression.IsSet())
        {
            return NeutralKey.EventSourceId;
        }

        if (TryUnwrap(expression.Value, WellKnownExpressions.Composite, out var composite))
        {
            var segments = composite.Split(", ");
            var parts = segments.Skip(1)
                .Select(_ => _.Split('=', 2))
                .Select(_ => (_[0], KeyValue(_[1])))
                .ToArray();
            return new(segments[0], parts);
        }

        return NeutralKey.Of(KeyValue(expression.Value));
    }

    static NeutralKey? ParentKey(PropertyExpression? expression, bool isChildContext)
    {
        if (expression?.IsSet() == true)
        {
            return Key(expression);
        }

        return isChildContext ? NeutralKey.EventSourceId : null;
    }

    static NeutralValue KeyValue(string expression) =>
        TryUnwrap(expression, WellKnownExpressions.Value, out var literal)
            ? new(NeutralValueKind.Text, literal)
            : Value(expression);

    static NeutralValue Value(string expression)
    {
        if (expression == WellKnownExpressions.EventSourceId)
        {
            return NeutralValue.EventSourceId;
        }

        if (TryUnwrap(expression, WellKnownExpressions.EventContext, out var context))
        {
            return context == "eventSourceId" ? NeutralValue.EventSourceId : new(NeutralValueKind.EventContext, context);
        }

        if (expression.StartsWith('$') || expression.StartsWith('`'))
        {
            return new(NeutralValueKind.Unsupported, expression);
        }

        if (expression.Length >= 2 && expression.StartsWith('"') && expression.EndsWith('"'))
        {
            return new(NeutralValueKind.Text, expression[1..^1]);
        }

        if (string.Equals(expression, "True", StringComparison.Ordinal) || string.Equals(expression, "False", StringComparison.Ordinal))
        {
            return new(NeutralValueKind.Boolean, expression.ToLowerInvariant());
        }

        return decimal.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? new(NeutralValueKind.Number, Number(number))
            : new(NeutralValueKind.EventProperty, expression);
    }

    static bool TryUnwrap(string expression, string function, out string argument)
    {
        argument = string.Empty;
        if (!expression.StartsWith($"{function}(", StringComparison.Ordinal) || !expression.EndsWith(')'))
        {
            return false;
        }

        argument = expression[(function.Length + 1)..^1];
        return true;
    }

    /// <summary>
    /// Formats a number canonically, so trailing zeros do not read as a difference.
    /// </summary>
    /// <param name="number">The number.</param>
    /// <returns>The canonical text.</returns>
    internal static string Number(decimal number) => number.ToString("G29", CultureInfo.InvariantCulture);
}
