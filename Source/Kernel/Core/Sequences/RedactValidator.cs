// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="Redact"/>.
/// </summary>
/// <remarks>
/// A redaction is a compliance operation that permanently removes payload availability, so the request
/// must say why - a redaction marker without a reason leaves the audit trail unable to explain itself.
/// </remarks>
internal class RedactValidator : CommandValidator<Redact>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedactValidator"/> class.
    /// </summary>
    public RedactValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(_ => _.Reason).NotEmpty().WithMessage("A reason for the redaction is required.");
    }
}
