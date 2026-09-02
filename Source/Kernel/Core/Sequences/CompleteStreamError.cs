// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the outcome of a rejected stream completion.
/// </summary>
/// <remarks>
/// Deliberately its own type rather than <see cref="Cratis.Chronicle.EventSequences.CompleteStreamError"/> directly
/// - the same reason <see cref="EventType"/> and <see cref="ConcurrencyScope"/> are their own local types: it keeps
/// this command's wire shape mirroring into this service's own <c>Contracts.Sequences</c> namespace, rather than
/// reaching into <c>Contracts.EventSequences</c>, where a hand-written contract of the same name still serves the
/// not-yet-retired <c>EventSequences</c> service.
/// </remarks>
public enum CompleteStreamError
{
    /// <summary>
    /// There was no error - the stream was completed successfully.
    /// </summary>
    /// <remarks>
    /// The wire representation of <see cref="CompleteStreamOutcome"/> carries this as a non-nullable value, so
    /// completion needs an explicit "no error" member rather than relying on a nullable enum - a nullable enum that
    /// also needs value conversion has no defined null behavior on the wire.
    /// </remarks>
    None = 0,

    /// <summary>
    /// The stream was already completed previously.
    /// </summary>
    AlreadyCompleted = 1,

    /// <summary>
    /// The default stream cannot be completed.
    /// </summary>
    DefaultStreamCannotBeCompleted = 2,
}
