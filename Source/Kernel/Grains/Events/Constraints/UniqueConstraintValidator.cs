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
    IUniqueConstraintsStorage storage) : IConstraintValidator
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => definition;

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) =>
        definition.EventDefinitions.Any(_ => _.EventType == context.EventType);

    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(ConstraintValidationContext context)
    {
        var (property, value) = GetPropertyAndValue(definition, context);
        if (value is null)
        {
            return ConstraintValidationResult.Success;
        }

        var (isAllowed, sequenceNumber) = await storage.IsAllowed(context.EventSourceId, definition.Name, value);
        return isAllowed ?
            ConstraintValidationResult.Success :
            new() { Violations = [this.CreateViolation(context, sequenceNumber, $"Event '{context.EventType}' with value '{value}' on property '{property}' violated a unique constraint on sequence number {sequenceNumber}")] };
    }

    /// <inheritdoc/>
    public async Task Update(ConstraintValidationContext context, EventSequenceNumber eventSequenceNumber)
    {
        var (_, value) = GetPropertyAndValue(definition, context);
        if (value is not null)
        {
            await storage.Save(context.EventSourceId, definition.Name, eventSequenceNumber, value);
        }
    }

    (string Property, string? Value) GetPropertyAndValue(UniqueConstraintDefinition definition, ConstraintValidationContext context)
    {
        var property = definition.EventDefinitions.Single(_ => _.EventType == context.EventType).Property;
        var contentAsDictionary = (context.Content as IDictionary<string, object>)!;
        var value = contentAsDictionary[property]?.ToString();

        return (property, value);
    }
}
