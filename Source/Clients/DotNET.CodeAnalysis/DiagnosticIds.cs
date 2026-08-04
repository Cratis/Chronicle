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

    /// <summary>
    /// A model-bound read model property has no provable mapping source (no mapping attribute and no
    /// subscribed event carrying a same-named property for AutoMap to bind).
    /// </summary>
    public const string ReadModelPropertyMustHaveMappingSource = "CHR0024";

    /// <summary>
    /// An explicitly sourced read model property is overwritten by AutoMap from another subscribed event
    /// that carries an identically named property.
    /// </summary>
    public const string AutoMapSameNamePropertyCollision = "CHR0025";

    /// <summary>
    /// A [Key] or [Subject] attribute is placed on a property whose type derives from EventSourceId&lt;T&gt;.
    /// </summary>
    public const string KeyOrSubjectOnEventSourceId = "CHR0026";

    /// <summary>
    /// A command both implements ICanProvideEventStreamId and carries a non-null [EventStreamId] attribute.
    /// </summary>
    public const string AmbiguousEventStreamId = "CHR0027";

    /// <summary>
    /// A projection Define() calls .AutoMap(), which is already the default and therefore redundant.
    /// </summary>
    public const string RedundantAutoMapCall = "CHR0028";

    /// <summary>
    /// A projection .Set(x =&gt; x.P).To(e =&gt; e.P) maps a property from an identically named event property,
    /// which AutoMap already does, and is therefore redundant.
    /// </summary>
    public const string RedundantSetToWithMatchingNames = "CHR0029";

    /// <summary>
    /// A [ChildrenFrom] child collection property has no matching event property and no explicit mapping, so it auto-maps to nothing.
    /// </summary>
    public const string ChildrenCollectionPropertyAutoMapsToNothing = "CHR0030";

    /// <summary>
    /// A reactor declares mutable instance state (a non-readonly field or a settable property).
    /// </summary>
    public const string ReactorMustNotHaveMutableState = "CHR0031";

    /// <summary>
    /// A reactor injects a storage primitive (e.g. IMongoCollection&lt;T&gt;) directly.
    /// </summary>
    public const string ReactorMustNotAccessStorageDirectly = "CHR0032";

    /// <summary>
    /// A [PII] attribute is placed on a property or parameter whose type derives from EventSourceId&lt;T&gt;.
    /// </summary>
    public const string PiiOnEventSourceId = "CHR0034";

    /// <summary>
    /// A read model declares a property named '_subject', which Chronicle reserves as an internal field.
    /// </summary>
    public const string ReservedSubjectProperty = "CHR0035";

    /// <summary>
    /// A reducer declares mutable instance state or injects a storage primitive directly.
    /// </summary>
    public const string ReducerMustNotHaveMutableState = "CHR0036";

    /// <summary>
    /// The event type generations referenced by an event type migration must share one explicit [EventType] id.
    /// </summary>
    public const string MigrationGenerationEventTypeId = "CHR0037";

    /// <summary>
    /// A model-bound [Join] copies a [PII] value from a stream keyed by something other than the read model's own compliance subject.
    /// </summary>
    public const string CrossSubjectPiiJoin = "CHR0038";

    /// <summary>
    /// A Task-returning testing assertion is discarded rather than awaited, so it can never fail.
    /// </summary>
    public const string NonAwaitedAssertion = "CHR0039";

    /// <summary>
    /// One member carries several [SetFromContext] for the same event type, so all but the last are discarded.
    /// </summary>
    public const string DuplicateSetFromContextForSameEventType = "CHR0040";
}
