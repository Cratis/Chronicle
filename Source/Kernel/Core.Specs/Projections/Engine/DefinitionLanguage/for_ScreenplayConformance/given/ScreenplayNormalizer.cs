// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// Normalizes a <see cref="SemanticProjection"/> bound by Screenplay's executable semantic model into the neutral tree.
/// </summary>
/// <remarks>
/// The semantic model names everything by <see cref="SemanticId"/>. The names are recovered by walking the application's
/// types and the slice's events and read models, because the index the binder keeps for this is internal.
/// A flat projection (no <see cref="SemanticProjection.Scope"/>) is normalized from its transitions: each is a
/// <c language="csharp">from</c> keyed by an event property, without a parent, at the projection's own level.
/// </remarks>
/// <param name="application">The compiled application.</param>
/// <param name="slice">The slice holding the case's declarations.</param>
public class ScreenplayNormalizer(SemanticApplication application, SemanticSlice slice)
{
    readonly Dictionary<SemanticId, string> _names = Names(application, slice);

    /// <summary>
    /// Normalizes a projection.
    /// </summary>
    /// <param name="projection">The projection to normalize.</param>
    /// <returns>The neutral projection level.</returns>
    public NeutralScope Normalize(SemanticProjection projection) =>
        projection.Scope is null ? Flat(projection.Transitions) : Scope(projection.Scope);

    static Dictionary<SemanticId, string> Names(SemanticApplication application, SemanticSlice slice)
    {
        var names = new Dictionary<SemanticId, string>();
        foreach (var type in application.Types)
        {
            names[type.Id] = type.Name;
            foreach (var property in type.Properties)
            {
                names[property.Id] = property.Name;
            }
        }

        foreach (var @event in slice.Events)
        {
            names[@event.Id] = @event.Name;
            foreach (var property in @event.Properties)
            {
                names[property.Id] = property.Name;
            }
        }

        foreach (var property in slice.ReadModels.SelectMany(_ => _.Properties))
        {
            names[property.Id] = property.Name;
        }

        return names;
    }

    NeutralScope Flat(ImmutableArray<SemanticProjectionTransition> transitions) =>
        new(
            [.. transitions.Select(_ => new NeutralFrom(Name(_.EventContract), NeutralKey.Of(Value(_.AffectedInstance.Key)), null, [.. _.Mappings.Select(Mapping)]))],
            [],
            [],
            [],
            null,
            [],
            []);

    NeutralScope Scope(SemanticProjectionScope scope)
    {
        var every = scope.Every is null || (scope.Every.Mappings.IsEmpty && !scope.Every.IncludeChildren && !scope.Every.SubscribesToAllEvents)
            ? null
            : new NeutralEvery(scope.Every.IncludeChildren, scope.Every.SubscribesToAllEvents, [.. scope.Every.Mappings.Select(Mapping)]);

        return new(
            [.. scope.From.Select(_ => new NeutralFrom(Name(_.EventContract), Key(_.Key), _.ParentKey is null ? null : Key(_.ParentKey), [.. _.Mappings.Select(Mapping)]))],
            [.. scope.Joins.Select(_ => new NeutralJoin(Name(_.EventContract), Name(_.On), _.Key is null ? null : Key(_.Key), [.. _.Mappings.Select(Mapping)]))],
            [.. scope.Children.Select(_ => new NeutralChildren(Name(_.Property), Name(_.IdentifiedBy), Scope(_.Scope)))],
            [.. scope.Nested.Select(_ => new NeutralNested(Name(_.Property), Scope(_.Scope)))],
            every,
            [.. scope.Removals.Select(_ => new NeutralRemoval(Name(_.EventContract), Key(_.Key), _.ParentKey is null ? null : Key(_.ParentKey)))],
            [.. scope.JoinRemovals.Select(_ => new NeutralJoinRemoval(Name(_.EventContract), Key(_.Key)))]);
    }

    NeutralMapping Mapping(SemanticProjectionMapping mapping) =>
        new(Path(mapping.Target), Operation(mapping.Operation), mapping.Source is null ? null : Value(mapping.Source));

    NeutralMapping Mapping(SemanticPropertyMapping mapping) =>
        mapping.Source is SemanticValueExpression { Value.Kind: SemanticValueKind.Null }
            ? new(Name(mapping.TargetProperty), NeutralOperation.Clear, null)
            : new(Name(mapping.TargetProperty), NeutralOperation.Set, Value(mapping.Source));

    NeutralKey Key(SemanticProjectionKey key) => key switch
    {
        SemanticProjectionValueKey value => NeutralKey.Of(Value(value.Value)),
        SemanticProjectionCompositeKey composite => new(Name(composite.Type), [.. composite.Parts.Select(_ => (Name(_.Property), Value(_.Value)))]),
        _ => throw new UnknownSemanticNode(key)
    };

    NeutralValue Value(SemanticProjectionValue value) => value switch
    {
        SemanticProjectionEventProperty property => new(NeutralValueKind.EventProperty, Path(property.Path)),
        SemanticProjectionEventSourceIdentity => NeutralValue.EventSourceId,
        SemanticProjectionEventContextValue context => new(NeutralValueKind.EventContext, context.Path),
        SemanticProjectionLiteral literal => Literal(literal.Value),
        _ => throw new UnknownSemanticNode(value)
    };

    NeutralValue Value(SemanticExpression expression) => expression switch
    {
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event } resolved => new(NeutralValueKind.EventProperty, Name(resolved.Target)),
        SemanticValueExpression value => Literal(value.Value),
        _ => throw new UnknownSemanticNode(expression)
    };

    NeutralValue Literal(SemanticValue value) => value switch
    {
        SemanticTextValue text => new(NeutralValueKind.Text, text.Value),
        SemanticNumberValue number => new(NeutralValueKind.Number, ChronicleNormalizer.Number(number.Value)),
        SemanticBooleanValue boolean => new(NeutralValueKind.Boolean, boolean.Value ? "true" : "false"),
        _ => throw new UnknownSemanticNode(value)
    };

    static NeutralOperation Operation(SemanticProjectionOperation operation) => operation switch
    {
        SemanticProjectionOperation.Set => NeutralOperation.Set,
        SemanticProjectionOperation.Clear => NeutralOperation.Clear,
        SemanticProjectionOperation.Add => NeutralOperation.Add,
        SemanticProjectionOperation.Subtract => NeutralOperation.Subtract,
        SemanticProjectionOperation.Increment => NeutralOperation.Increment,
        SemanticProjectionOperation.Decrement => NeutralOperation.Decrement,
        _ => throw new UnknownSemanticNode(operation)
    };

    string Path(ImmutableArray<SemanticId> path) => string.Join('.', path.Select(Name));

    string Name(SemanticId id) => _names.TryGetValue(id, out var name) ? name : throw new UnknownSemanticNode(id);
}
