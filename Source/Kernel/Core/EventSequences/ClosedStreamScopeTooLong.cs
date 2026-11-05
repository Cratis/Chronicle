// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when the identifiers of a stream to complete are too long to be stored.
/// </summary>
public class ClosedStreamScopeTooLong()
    : Exception($"The stream scope is too long to be stored: its identifiers may total at most {ClosedStreamKeyLength.MaxTotalLength} characters and each at most {ClosedStreamKeyLength.MaxColumnLength}"), IValidationFailure
{
    static readonly string[] _members = ["scope"];

    /// <inheritdoc/>
    public ValidationResult ValidationResult => new(ValidationResultSeverity.Error, Message, _members, null!);
}
