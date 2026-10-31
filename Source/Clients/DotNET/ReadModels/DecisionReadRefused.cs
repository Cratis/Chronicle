// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown before enrollment when a decision read cannot be protected.</summary>
/// <param name="reason">The refusal reason.</param>
/// <param name="readModelType">The read model type.</param>
/// <param name="detail">Optional diagnostic detail without definition values or event content.</param>
public class DecisionReadRefused(DecisionReadRefusalReason reason, Type readModelType, string? detail)
    : Exception($"Decision read of '{readModelType}' was refused: {reason}. {Explain(reason)} {detail}".TrimEnd())
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionReadRefused"/> class.
    /// </summary>
    /// <param name="reason">The refusal reason.</param>
    /// <param name="readModelType">The read model type.</param>
    public DecisionReadRefused(DecisionReadRefusalReason reason, Type readModelType)
        : this(reason, readModelType, null)
    {
    }

    /// <summary>Gets the refusal reason.</summary>
    public DecisionReadRefusalReason Reason { get; } = reason;

    /// <summary>Gets the refused model type.</summary>
    public Type ReadModelType { get; } = readModelType;

    static string Explain(DecisionReadRefusalReason reason) => reason switch
    {
        DecisionReadRefusalReason.Reducer =>
            "The read model is produced by a reducer, and a decision read can only fold a projection. Read a read model that has a projection instead.",
        DecisionReadRefusalReason.AmbiguousProjection =>
            "A decision read needs exactly one projection for the read model, and there is none or more than one.",
        DecisionReadRefusalReason.NotEventLog =>
            "The projection observes an event sequence other than the event log.",
        DecisionReadRefusalReason.Join =>
            "The projection joins state from other event sources, which a decision read cannot guard. Read a read model without joins instead.",
        DecisionReadRefusalReason.Hierarchy =>
            "The projection has children or nested objects, and a decision read only folds flat projections. Read a read model whose projection has no children or nested objects instead.",
        DecisionReadRefusalReason.OpenEndedEventTypes =>
            "The projection subscribes to all events, so there is no closed set of event types to guard.",
        DecisionReadRefusalReason.Derivatives =>
            "The projection includes derived event types, so there is no closed set of event types to guard.",
        DecisionReadRefusalReason.FromEventProperty =>
            "The projection routes events by an event property instead of the event source id.",
        DecisionReadRefusalReason.NotEventSourceKeyed =>
            "The projection keys or routes events by something other than the event source id, for example a key or parent key taken from the event.",
        DecisionReadRefusalReason.KeyConversion =>
            "The read model needs a key property of type string or Guid, used as the event source id without conversion.",
        DecisionReadRefusalReason.NoEventTypes =>
            "The projection has no event types to guard.",
        DecisionReadRefusalReason.UnsupportedEventTypeId =>
            "An event type id of the projection contains a comma, which the guard cannot encode.",
        DecisionReadRefusalReason.InvalidKey =>
            "The key must be a single event source id: not empty, not a wildcard, without '#' or surrounding whitespace, and, for a Guid-keyed read model, in lowercase canonical form.",
        DecisionReadRefusalReason.FoldIncomplete =>
            "The fold did not reach the latest matching event. Try again.",
        DecisionReadRefusalReason.DefinitionMismatch =>
            "The kernel's listed definition differs from the client's. Check the reported differences and align the client and kernel definitions before retrying.",
        DecisionReadRefusalReason.DefinitionMissing =>
            "The kernel has no definition for the read model or its projection. Ensure the client's artifact registration has completed before retrying.",
        DecisionReadRefusalReason.StreamScopeNotSupported =>
            "The reader or kernel does not support stream-scoped decision reads. Use a reader and kernel that acknowledge the requested stream scope.",
        DecisionReadRefusalReason.StreamKeyRequiresStreamScope =>
            "The projection is keyed by event stream id. Use the stream-scoped read overload with an event source id, stream type and stream id.",
        DecisionReadRefusalReason.AllStreamsNotSupported =>
            "A stream-scoped decision read cannot select all stream types. Supply a specific event stream type, or use a source-wide read for an event-source-keyed projection.",
        DecisionReadRefusalReason.StreamKeyRequiresExplicitStreamId =>
            "A stream-keyed projection cannot use the default stream id because it does not narrow the fold to one instance key. Supply an explicit event stream id.",
        DecisionReadRefusalReason.GenerationDeliveryNotSupported =>
            "The session fold uses Compatibility delivery and cannot honor a Pinned projection. Use Compatibility delivery for this projection.",
        DecisionReadRefusalReason.ReducerDependencies =>
            "The reducer has constructor dependencies. Only ILogger<T> infrastructure dependencies are allowed.",
        DecisionReadRefusalReason.ReducerFilters =>
            "reducer filters its events",
        DecisionReadRefusalReason.ReducerGeneration =>
            "The handler's declared event generation is unavailable or the event was revised to another generation.",
        DecisionReadRefusalReason.ReducerDispatchIncomplete =>
            "The reducer did not dispatch every fetched event of its handled types.",
        DecisionReadRefusalReason.ReadModelRequiresStreamScope =>
            "The read model is scoped to one event stream and was read without it. Use a stream-scoped decision read, or supply the event source id and stream id to the plain instance read.",
        DecisionReadRefusalReason.StreamScopeDoesNotMatchReadModel =>
            "The supplied stream type or event source type does not match the read model's declaration. Pass the declared stream and the event source definition's source type.",
        _ => string.Empty
    };
}
