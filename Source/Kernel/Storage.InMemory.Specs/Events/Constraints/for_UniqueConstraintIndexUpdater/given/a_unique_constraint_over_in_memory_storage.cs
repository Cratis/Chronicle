// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintIndexUpdater.given;

/// <summary>
/// Wires the real unique constraint validator and index updater to the in-memory storage, so specs can
/// append values for event sources the way the kernel does: validate first, then update the index.
/// </summary>
public class a_unique_constraint_over_in_memory_storage : Specification
{
    protected UniqueConstraintDefinition _definition;
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintValidator _validator;

    void Establish()
    {
        _storage = new();
        _definition = new("SomeConstraint", [new("SomeEvent", ["SomeProperty"])]);
        _validator = new(_definition, _storage);
    }

    /// <summary>
    /// Validates an append of the value for the event source and, when accepted, updates the index.
    /// </summary>
    /// <param name="eventSourceId">The event source appending.</param>
    /// <param name="value">The value of the constrained property, null for a missing value.</param>
    /// <param name="sequenceNumber">The sequence number of the event.</param>
    /// <returns>The validation result.</returns>
    protected async Task<ConstraintValidationResult> Append(EventSourceId eventSourceId, string? value, ulong sequenceNumber)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["SomeProperty"] = value;

        var context = new ConstraintValidationContext([], eventSourceId, "SomeEvent", content);
        var result = await _validator.Validate(context);
        if (result.IsValid)
        {
            await new UniqueConstraintIndexUpdater(_definition, context, _storage).Update(sequenceNumber);
        }

        return result;
    }
}
