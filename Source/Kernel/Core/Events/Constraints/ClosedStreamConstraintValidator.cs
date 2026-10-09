// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an <see cref="IConstraintValidator"/> that rejects appends covered by closed scopes.
/// </summary>
/// <param name="storage">The closure storage.</param>
/// <param name="dimensionsInUse">The known masks in storage.</param>
public class ClosedStreamConstraintValidator(IClosedStreamsConstraintStorage storage, IEnumerable<ClosedStreamDimensions> dimensionsInUse) : IConstraintValidator
{
    readonly ClosedStreamDimensions[] _dimensionsInUse = dimensionsInUse.Distinct().ToArray();

    /// <inheritdoc/>
    public IConstraintDefinition Definition { get; } = new ClosedStreamConstraintDefinition();

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) => true;

    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(ConstraintValidationContext context)
    {
        if (_dimensionsInUse.Length == 0) return ConstraintValidationResult.Success;

        var scope = new ClosedStreamScope(context.EventSourceId, context.EventSourceType, context.EventStreamType, context.EventStreamId);
        var closures = await storage.GetCovering(scope, _dimensionsInUse);

        return new()
        {
            Violations = closures.Select(closure => this.CreateViolation(
                context,
                closure.SequenceNumber,
                $"Stream '{context.EventStreamType}/{context.EventStreamId}' is closed and cannot accept new events",
                new()
                {
                    { WellKnownConstraintDetailKeys.ClosedBy, closure.Owner.Value },
                    { WellKnownConstraintDetailKeys.ClosedAtSequenceNumber, closure.SequenceNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                })).ToImmutableList()
        };
    }
}
