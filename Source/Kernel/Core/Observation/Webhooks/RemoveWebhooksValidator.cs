// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using FluentValidation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the validator for <see cref="RemoveWebhooks"/>.
/// </summary>
internal class RemoveWebhooksValidator : CommandValidator<RemoveWebhooks>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveWebhooksValidator"/> class.
    /// </summary>
    public RemoveWebhooksValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Webhooks).NotEmpty().WithMessage("At least one webhook is required.");

        // RuleForEach's own NotEmpty() checks the element reference against null/default, not the wrapped
        // value - WebhookId is a non-null record even when its Value is empty.
        RuleForEach(_ => _.Webhooks).Must(id => id != WebhookId.Unspecified).WithMessage("Webhook identifier cannot be empty.");
    }
}
