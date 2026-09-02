// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the validator for <see cref="RemoveWebhook"/>.
/// </summary>
internal class RemoveWebhookValidator : CommandValidator<RemoveWebhook>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveWebhookValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check for the webhook's existence in.</param>
    public RemoveWebhookValidator(IStorage storage)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.WebhookId).NotEmpty().WithMessage("Webhook identifier is required.");

        // A webhook's existence is scoped to its event store, so this is a command-level check rather than a
        // cross-cutting ConceptValidator<WebhookId> - a standalone concept validator has no visibility into
        // the sibling EventStore.
        RuleFor(_ => _)
            .MustAsync(async (command, _) => await storage.GetEventStore(command.EventStore).Webhooks.Has(command.WebhookId))
            .WithMessage("Webhook does not exist.");
    }
}
