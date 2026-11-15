// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the validator for <see cref="AddImportChunk"/>.
/// </summary>
internal class AddImportChunkValidator : CommandValidator<AddImportChunk>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddImportChunkValidator"/> class.
    /// </summary>
    public AddImportChunkValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.EventStore).NoKeySeparator();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.Namespace).NoKeySeparator();
        RuleFor(_ => _.ChunkIndex).GreaterThanOrEqualTo(0).WithMessage("The chunk index must not be negative.");
        RuleFor(_ => _.FirstEventIndex).GreaterThanOrEqualTo(0).WithMessage("The index of the first event must not be negative.");
        RuleFor(_ => _.Events).RequiredEvents();
        RuleForEach(_ => _.Events).ChildRules(entry =>
        {
            entry.RuleFor(_ => _.EventType).RequiredEventType();
            entry.RuleFor(_ => _.EventType.Id).RequiredEventTypeId().When(_ => _.EventType is not null);
        });
    }
}
