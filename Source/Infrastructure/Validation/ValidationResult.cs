// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Validation;

/// <summary>
/// Represents the an failed validation rule.
/// </summary>
/// <param name="Severity">The <see cref="ValidationResultSeverity"/> of the result.</param>
/// <param name="Message">Message of the error.</param>
/// <param name="Members">Collection of member names that caused the failure.</param>
/// <param name="State">State associated with the validation result.</param>
public record ValidationResult(ValidationResultSeverity Severity, string Message, IEnumerable<string> Members, object State);
