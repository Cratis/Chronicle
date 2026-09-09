// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that needs to see every event within a batch append, not only the ones it validates.
/// </summary>
/// <remarks>
/// A batch is validated sequentially with a single shared <see cref="ConstraintBatchClaims"/> before any of it
/// is written, so a constraint whose answer depends on the relative order of two different event types within
/// the batch (an event that releases a cycle and a covered event that starts the next one) cannot rely solely
/// on <see cref="IConstraintValidator.CanValidate"/> - that would only ever see the covered side. Implementing
/// this lets a validator record what an event it does not otherwise validate means for the shared batch state,
/// so a later event in the same batch sees the effect before any of it is durably appended.
/// <para>
/// Internal: this is a collaboration detail between <see cref="ConstraintValidationContext"/> and the
/// validators Core itself ships (currently only <see cref="UniqueEventTypeConstraintValidator"/>), not a
/// documented extension point. It is not part of the public constraint-validator contract.
/// </para>
/// </remarks>
internal interface IObserveConstraintBatchEvents
{
    /// <summary>
    /// Record the effect of an event on the shared <see cref="ConstraintBatchClaims"/> for the batch it belongs to.
    /// </summary>
    /// <param name="context">The <see cref="ConstraintValidationContext"/> for the event.</param>
    /// <remarks>
    /// Called after all validators accept an event in a batch append, regardless of
    /// <see cref="IConstraintValidator.CanValidate"/>, and in the same order the events are validated. An event's
    /// effects are visible only to subsequent events, never its own validation. Never called for a failed
    /// validation or when validating a single, non-batched append -
    /// <see cref="ConstraintValidationContext.BatchClaims"/> is <see langword="null"/> in that case, so there is
    /// no shared state to observe.
    /// </remarks>
    void RecordBatchEvent(ConstraintValidationContext context);
}
