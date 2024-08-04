// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintValidatorSet"/>.
/// </summary>
/// <param name="validators">Collection of <see cref="IConstraintValidator"/> to use.</param>
public class ConstraintValidatorSet(IEnumerable<IConstraintValidator> validators) : IConstraintValidatorSet
{
    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(EventToValidateForConstraints eventToValidate)
    {
        var context = new ConstraintValidationContext(
            eventToValidate.EventSourceId,
            eventToValidate.EventType,
            eventToValidate.Content);
        var validatorsThatCanValidate = validators.Where(v => v.CanValidate(context));
        var results = await Task.WhenAll(validatorsThatCanValidate.Select(v => v.Validate(context)));
        var violations = results.Where(r => !r.IsValid).SelectMany(r => r.Violations);
        return new()
        {
            Violations = violations.ToImmutableList()
        };
    }

    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(IEnumerable<EventToValidateForConstraints> eventsToValidate)
    {
        var tasks = eventsToValidate.Select(Validate);
        var results = await Task.WhenAll(tasks);

        var violations = results.SelectMany(r => r.Violations);
        return new ConstraintValidationResult
        {
            Violations = violations.ToImmutableList()
        };
    }
}
