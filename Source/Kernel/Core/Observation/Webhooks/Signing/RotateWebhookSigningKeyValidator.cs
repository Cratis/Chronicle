// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Validates the event store and bounded overlap before rotating a signing key.
/// </summary>
internal class RotateWebhookSigningKeyValidator : CommandValidator<RotateWebhookSigningKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RotateWebhookSigningKeyValidator"/> class.
    /// </summary>
    public RotateWebhookSigningKeyValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).Must(store => !string.IsNullOrWhiteSpace(store)).WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.OverlapSeconds).Must(WebhookSigningKeyOperations.IsValidOverlap).WithMessage("Overlap must be finite and between zero and 30 days.");
    }
}
