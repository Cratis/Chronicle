// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using FluentValidation;

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents a validator for <see cref="AddWebhook"/>.
/// </summary>
internal class AddWebhookValidator : CommandValidator<AddWebhook>
{
    readonly IWebhooks _webhooks;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddWebhookValidator"/> class.
    /// </summary>
    /// <param name="webhooks"><see cref="IWebhooks"/> for working with webhooks.</param>
    public AddWebhookValidator(IWebhooks webhooks)
    {
        When(_ => _.AuthorizationType.Equals("oauth", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(_ => _.OAuthAuthority)
                .NotEmpty()
                .WithMessage("OAuth Authority is required when using OAuth authorization.");

            RuleFor(_ => _.OAuthClientId)
                .NotEmpty()
                .WithMessage("OAuth Client ID is required when using OAuth authorization.");

            RuleFor(_ => _.OAuthClientSecret)
                .NotEmpty()
                .WithMessage("OAuth Client Secret is required when using OAuth authorization.");
        });

        RuleFor(_ => _)
            .MustAsync(BeValidOAuthConfiguration)
            .WithMessage("Unable to acquire a valid OAuth token.");
        _webhooks = webhooks;
    }

    async Task<bool> BeValidOAuthConfiguration(AddWebhook command, CancellationToken cancellationToken)
    {
        if (!command.AuthorizationType.Equals("oauth", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var result = await _webhooks.TestOAuthAuthorization(
            new TestOAuthAuthorizationRequest
            {
                Authority = command.OAuthAuthority!,
                ClientId = command.OAuthClientId!,
                ClientSecret = command.OAuthClientSecret!
            },
            cancellationToken);

        return result.Success;
    }
}
