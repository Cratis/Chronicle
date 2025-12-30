// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.InProcess;

/// <summary>
/// Represents the settings for connecting to Chronicle.
/// </summary>
public class ChronicleOrleansInProcessOptions : IValidatableObject
{
    /// <summary>
    /// Gets the <see cref="Chronicle.EventStoreName"/> to use.
    /// </summary>
    [Required]
    public EventStoreName EventStoreName { get; set; } = EventStoreName.NotSet;

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(EventStoreName.Value) || EventStoreName == EventStoreName.NotSet)
        {
            yield return new ValidationResult(
                "EventStoreName must be configured and cannot be empty or '[NotSet]'. Configure it using AddCratisChronicle(options => options.EventStoreName = \"your-store-name\")",
                [nameof(EventStoreName)]);
        }
    }
}
