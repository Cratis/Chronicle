// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the validator for <see cref="RemoveExternalService"/>.
/// </summary>
internal class RemoveExternalServiceValidator : CommandValidator<RemoveExternalService>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveExternalServiceValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check for the external service's existence in.</param>
    public RemoveExternalServiceValidator(IStorage storage)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.ExternalServiceId).NotEmpty().WithMessage("External service identifier is required.");

        // EventStore and ExternalServiceId are checked together here, rather than through a cross-cutting
        // ConceptValidator<ExternalServiceId>, because an external service's existence is scoped to the event
        // store it belongs to - a standalone concept validator has no visibility into the sibling EventStore.
        RuleFor(_ => _)
            .MustAsync(async (command, _) => await storage.GetEventStore(command.EventStore).ExternalServices.Has(command.ExternalServiceId))
            .WithMessage("External service does not exist.");
    }
}
