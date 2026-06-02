// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an <see cref="IConstraintValidator"/> that rejects appends to closed event streams.
/// </summary>
/// <param name="storage">The <see cref="IClosedStreamsConstraintStorage"/> to use.</param>
public class ClosedStreamConstraintValidator(IClosedStreamsConstraintStorage storage) : IConstraintValidator
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition { get; } = new ClosedStreamConstraintDefinition();

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) =>
        context.EventStreamType is not null
        && context.EventStreamId is not null
        && !(context.EventStreamType == EventStreamType.All && context.EventStreamId.Value == EventStreamId.Default);

    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(ConstraintValidationContext context)
    {
        var isClosed = await storage.IsStreamClosed(context.EventStreamType!, context.EventStreamId!);
        if (!isClosed)
        {
            return ConstraintValidationResult.Success;
        }

        return new()
        {
            Violations =
            [
                this.CreateViolation(
                    context,
                    EventSequenceNumber.Unavailable,
                    $"Stream '{context.EventStreamType}/{context.EventStreamId}' is closed and cannot accept new events")
            ]
        };
    }
}
