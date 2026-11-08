// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the validator for <see cref="CompleteImport"/>.
/// </summary>
internal class CompleteImportValidator : CommandValidator<CompleteImport>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteImportValidator"/> class.
    /// </summary>
    public CompleteImportValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.ExpectedChunks).GreaterThanOrEqualTo(0).WithMessage("The expected chunk count must not be negative.");
        RuleFor(_ => _.ExpectedEvents).GreaterThanOrEqualTo(0).WithMessage("The expected event count must not be negative.");
    }
}
