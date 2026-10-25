// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the validator for <see cref="AddWebhooks"/>.
/// </summary>
/// <remarks>
/// Only the values the definition carries are checked here. Reaching the endpoint and testing its authorization is what <see cref="AddWebhookValidator"/> does for a single webhook, and it is deliberately not repeated per element for a bulk registration.
/// </remarks>
internal class AddWebhooksValidator : CommandValidator<AddWebhooks>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddWebhooksValidator"/> class.
    /// </summary>
    /// <param name="policy">The shared webhook and OAuth target policy.</param>
    public AddWebhooksValidator(IWebhookTargetPolicy policy)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Webhooks).NotEmpty().WithMessage("At least one webhook is required.");
        RuleForEach(_ => _.Webhooks).ChildRules(webhook =>
        {
            webhook.RuleFor(_ => _.Identifier).NotEmpty().WithMessage("Webhook identifier is required.");
            webhook.RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        });
        RuleFor(_ => _).CustomAsync(async (command, context, cancellationToken) =>
        {
            if (command.Webhooks is null) return;
            foreach (var definition in command.Webhooks)
            {
                var target = await policy.Evaluate(definition.Target.Url, command.EventStore, cancellationToken);
                if (!target.IsAllowed) context.AddFailure("Webhooks.Target.Url", target.Reason);
                if (definition.Target.Authorization?.Value2 is { } oauth)
                {
                    var authority = await policy.Evaluate(oauth.Authority, command.EventStore, cancellationToken);
                    if (!authority.IsAllowed) context.AddFailure("Webhooks.Target.Authorization", authority.Reason);
                }
            }
        });
    }
}
