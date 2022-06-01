// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Aksio.Cratis.Events.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Represents a single business rule.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
public abstract class RuleAttribute : ValidationAttribute, IRule
{
    /// <summary>
    /// Gets the unique identifier for the business rules.
    /// </summary>
    public abstract RuleId Identifier { get; }

    /// <summary>
    /// Gets whether or not value adorned represents the <see cref="ModelKey"/>.
    /// </summary>
    public bool IsModelKey { get; set; }

    /// <summary>
    /// Validates the value it was adorned.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <returns>True if valid, false if not.</returns>
    protected new virtual bool IsValid(object? value) => true;

    /// <summary>
    /// Validates the value it was adorned.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <param name="value">Value to validate.</param>
    /// <returns>True if valid, false if not.</returns>
    protected virtual ValidationResult IsValid(ValidationContext validationContext, object? value) => ValidationResult.Success!;

    /// <inheritdoc/>
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var rules = validationContext.GetService<IRules>()!;
        rules.ProjectTo(this, IsModelKey ? value : null!);

        if (!IsValid(value))
        {
            return new ValidationResult(ErrorMessage);
        }

        return IsValid(validationContext, value);
    }
}
