// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the validator for <see cref="BeginImport"/>.
/// </summary>
internal class BeginImportValidator : CommandValidator<BeginImport>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BeginImportValidator"/> class.
    /// </summary>
    public BeginImportValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.EventStore).NoKeySeparator();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.Namespace).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
        RuleFor(_ => _.EventSequenceId).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId)
            .Must(_ => _ != EventSequenceId.Outbox)
            .WithMessage("Events cannot be imported into the outbox; it holds public events produced from private ones.");
    }
}
