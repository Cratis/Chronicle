// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintValidator"/> for unique constraints.
/// </summary>
/// <param name="definition">The <see cref="UniqueConstraintDefinition"/> for the validator.</param>
/// <param name="storage">The <see cref="IUniqueConstraintsStorage"/> to use.</param>
public class UniqueConstraintValidator(
    UniqueConstraintDefinition definition,
    IUniqueConstraintsStorage storage) : IConstraintValidator, IHaveUpdateConstraintIndex
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => definition;

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) =>
        definition.EventDefinitions.Any(_ => _.EventTypeId == context.EventTypeId);

    /// <inheritdoc/>
    public IUpdateConstraintIndex GetUpdateFor(ConstraintValidationContext context) => new UniqueConstraintIndexUpdater(definition, context, storage);

    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(ConstraintValidationContext context)
    {
        var (property, value) = definition.GetPropertyAndValue(context);
        if (value is null)
        {
            return ConstraintValidationResult.Success;
        }

        var (isAllowed, sequenceNumber) = await storage.IsAllowed(context.EventSourceId, definition.Name, value);
        return isAllowed ?
            ConstraintValidationResult.Success :
            new()
            {
                Violations =
                [
                    this.CreateViolation(
                        context,
                        sequenceNumber,
                        $"Event '{context.EventTypeId}' with value '{value}' on member '{property}' violated a unique constraint on sequence number {sequenceNumber}",
                        new() { { WellKnownConstraintDetailKeys.PropertyName, property }, { WellKnownConstraintDetailKeys.PropertyValue, value } })
                ]
            };
    }
}

public record TombstoneConstraintDefinition : IConstraintDefinition
{
    public string Name { get; init; } = default!;
    public IEnumerable<EventDefinition> EventDefinitions { get; init; } = default!;
}

/// <summary>
/// Represents an implementation of <see cref="IConstraintValidator"/> for tombstone constraints.
/// </summary>
public class TombstoneConstraintValidator : IConstraintValidator, IHaveUpdateConstraintIndex
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUpdateConstraintIndex GetUpdateFor(ConstraintValidationContext context) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<ConstraintValidationResult> Validate(ConstraintValidationContext context) => throw new NotImplementedException();
}


public class TombstoneConstraintIndexUpdater : IUpdateConstraintIndex
{
    public Task Update(EventSequenceNumber eventSequenceNumber) => throw new NotImplementedException();
}
