// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Diagnostic IDs for Chronicle analyzers.
/// </summary>
public static class DiagnosticIds
{
    /// <summary>
    /// Event type must have EventType attribute when appended to event sequence.
    /// </summary>
    public const string EventTypeMustHaveAttributeWhenAppended = "CHR0001";

    /// <summary>
    /// Declarative projection generic arguments must have EventType attribute.
    /// </summary>
    public const string DeclarativeProjectionEventTypeMustHaveAttribute = "CHR0002";

    /// <summary>
    /// Model bound projection attributes must reference types with EventType attribute.
    /// </summary>
    public const string ModelBoundProjectionEventTypeMustHaveAttribute = "CHR0003";

    /// <summary>
    /// Reactor method parameters after the event must be the EventContext, a read model, or a service.
    /// </summary>
    public const string ReactorMethodSignatureMustMatchAllowed = "CHR0004";

    /// <summary>
    /// Reactor event parameter must have EventType attribute.
    /// </summary>
    public const string ReactorEventParameterMustHaveAttribute = "CHR0005";

    /// <summary>
    /// Reducer method signature must match allowed signatures.
    /// </summary>
    public const string ReducerMethodSignatureMustMatchAllowed = "CHR0006";

    /// <summary>
    /// Reducer event parameter must have EventType attribute.
    /// </summary>
    public const string ReducerEventParameterMustHaveAttribute = "CHR0007";

    /// <summary>
    /// Reactor event types must all be from the same event store.
    /// </summary>
    public const string ReactorEventTypesMustBeFromSameEventStore = "CHR0008";

    /// <summary>
    /// Reducer event types must all be from the same event store.
    /// </summary>
    public const string ReducerEventTypesMustBeFromSameEventStore = "CHR0009";

    /// <summary>
    /// Model-bound projection event types must all be from the same event store.
    /// </summary>
    public const string ModelBoundProjectionEventTypesMustBeFromSameEventStore = "CHR0010";

    /// <summary>
    /// Declarative projection event types must all be from the same event store.
    /// </summary>
    public const string DeclarativeProjectionEventTypesMustBeFromSameEventStore = "CHR0011";

    /// <summary>
    /// Event types should avoid nullable properties.
    /// </summary>
    public const string EventTypeShouldAvoidNullableProperties = "CHR0012";

    /// <summary>
    /// Reactor cannot combine EventStore attribute with an explicit event sequence.
    /// </summary>
    public const string ReactorCannotCombineEventStoreWithExplicitEventSequence = "CHR0013";

    /// <summary>
    /// Reducer cannot combine EventStore attribute with an explicit event sequence.
    /// </summary>
    public const string ReducerCannotCombineEventStoreWithExplicitEventSequence = "CHR0014";

    /// <summary>
    /// Projection must not have side effects (inject ICommandPipeline or IEventLog).
    /// </summary>
    public const string ProjectionMustNotHaveSideEffects = "CHR0015";

    /// <summary>
    /// Projection Define() method must not contain imperative code.
    /// </summary>
    public const string ProjectionDefineMustNotContainImperativeCode = "CHR0016";

    /// <summary>
    /// Constraint must not have side effects (inject ICommandPipeline or IEventLog).
    /// </summary>
    public const string ConstraintMustNotHaveSideEffects = "CHR0017";

    /// <summary>
    /// Constraint Define() method must not contain imperative code.
    /// </summary>
    public const string ConstraintDefineMustNotContainImperativeCode = "CHR0018";

    /// <summary>
    /// Projection expression lambdas must only access members (no method calls, computations, or conditionals).
    /// </summary>
    public const string ProjectionExpressionLambdaMustOnlyAccessMembers = "CHR0019";

    /// <summary>
    /// Constraint expression lambdas must only access members (no method calls, computations, or conditionals).
    /// </summary>
    public const string ConstraintExpressionLambdaMustOnlyAccessMembers = "CHR0020";

    /// <summary>
    /// Event types should be declared as record types for immutability.
    /// </summary>
    public const string EventTypeShouldBeRecordType = "CHR0021";

    /// <summary>
    /// Reactor methods that return event side effects must be marked with [OnceOnly] attribute.
    /// </summary>
    public const string ReactorReturningEventsMustBeOnceOnly = "CHR0022";

    /// <summary>
    /// A [ChildrenFrom] collection that omits parentKey has an ambiguous parent key inference.
    /// </summary>
    public const string AmbiguousChildrenParentKey = "CHR0023";
}
