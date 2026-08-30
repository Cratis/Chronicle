// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the validator for <see cref="DeleteCapture"/>.
/// </summary>
internal class DeleteCaptureValidator : CommandValidator<DeleteCapture>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCaptureValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check for the capture's existence in.</param>
    public DeleteCaptureValidator(IStorage storage)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.CaptureId).NotEmpty().WithMessage("Capture identifier is required.");

        // A capture's existence is scoped to its event store, so this is a command-level check rather than a
        // cross-cutting ConceptValidator<CaptureId> - a standalone concept validator has no visibility into
        // the sibling EventStore.
        RuleFor(_ => _)
            .MustAsync(async (command, _) => await storage.GetEventStore(command.EventStore).Captures.Has(command.CaptureId))
            .WithMessage("Capture does not exist.");
    }
}
