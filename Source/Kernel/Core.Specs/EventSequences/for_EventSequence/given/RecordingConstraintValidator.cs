// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// A constraint validator that never rejects but exposes a <see cref="RecordingConstraintIndexUpdater"/> so a spec can observe
/// the <see cref="EventSequenceNumber"/> the constraint index is updated with after an append.
/// </summary>
/// <param name="recordedSequenceNumbers">The sink that captures every sequence number the index is updated with.</param>
public class RecordingConstraintValidator(IList<EventSequenceNumber> recordedSequenceNumbers) : IConstraintValidator, IHaveUpdateConstraintIndex
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => throw new NotSupportedException();

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) => false;

    /// <inheritdoc/>
    public Task<ConstraintValidationResult> Validate(ConstraintValidationContext context) => Task.FromResult(ConstraintValidationResult.Success);

    /// <inheritdoc/>
    public IUpdateConstraintIndex GetUpdateFor(ConstraintValidationContext context) => new RecordingConstraintIndexUpdater(recordedSequenceNumbers);
}
