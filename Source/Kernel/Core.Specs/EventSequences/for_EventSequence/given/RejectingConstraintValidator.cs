// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// A constraint validator that always rejects, used to prove that an append is validated against a newly enforced constraint.
/// </summary>
public class RejectingConstraintValidator : IConstraintValidator
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => throw new NotSupportedException();

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) => true;

    /// <inheritdoc/>
    public Task<ConstraintValidationResult> Validate(ConstraintValidationContext context) =>
        Task.FromResult(ConstraintValidationResult.Failed(
        [
            new ConstraintViolation(
                context.EventTypeId,
                EventSequenceNumber.Unavailable,
                ConstraintType.Unique,
                "unique-thing",
                "rejected by a constraint registered after activation",
                new ConstraintViolationDetails())
        ]));
}
