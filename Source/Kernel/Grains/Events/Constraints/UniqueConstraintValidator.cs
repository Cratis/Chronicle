// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
        var propertiesWithValues = definition.GetPropertiesAndValues(context).ToList();
        if (propertiesWithValues.Count == 0)
        {
            return ConstraintValidationResult.Success;
        }

        var value = propertiesWithValues.GetValue();
        var (isAllowed, sequenceNumber) = await storage.IsAllowed(context.EventSourceId, definition.Name, value);
        return isAllowed ?
            ConstraintValidationResult.Success :
            new()
            {
                Violations = propertiesWithValues.Select(pv =>
                    this.CreateViolation(
                        context,
                        sequenceNumber,
                        $"Event '{context.EventTypeId}' with value '{pv.Value}' on member '{pv.Property}' violated a unique constraint on sequence number {sequenceNumber}",
                        new() { { WellKnownConstraintDetailKeys.PropertyName, pv.Property }, { WellKnownConstraintDetailKeys.PropertyValue, value } })).ToImmutableList()
            };
    }
}
