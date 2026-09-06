// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
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
    public AddWebhooksValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Webhooks).NotEmpty().WithMessage("At least one webhook is required.");
        RuleForEach(_ => _.Webhooks).ChildRules(webhook =>
        {
            webhook.RuleFor(_ => _.Identifier).NotEmpty().WithMessage("Webhook identifier is required.");
            webhook.RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        });
    }
}
