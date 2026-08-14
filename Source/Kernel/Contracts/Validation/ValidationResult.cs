// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Validation;

/// <summary>
/// Wire-level representation of a failed validation rule.
/// </summary>
[ProtoContract]
public class ValidationResult
{
    /// <summary>
    /// Gets or sets the <see cref="ValidationResultSeverity"/> of the result.
    /// </summary>
    [ProtoMember(1)]
    [DefaultValue(ValidationResultSeverity.Error)]
    public ValidationResultSeverity Severity { get; set; } = ValidationResultSeverity.Error;

    /// <summary>
    /// Gets or sets the message describing the failure.
    /// </summary>
    [ProtoMember(2)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of member names that caused the failure.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<string> Members { get; set; } = [];
}
