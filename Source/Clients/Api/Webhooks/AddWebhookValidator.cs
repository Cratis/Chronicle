// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents a validator for <see cref="AddWebhook"/>.
/// </summary>
internal class AddWebhookValidator : CommandValidator<AddWebhook>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddWebhookValidator"/> class.
    /// </summary>
    public AddWebhookValidator()
    {
        RuleFor(_ => _)
            .Must(BeValidOAuthConfiguration)
            .WithMessage("OAuth configuration is incomplete. Authority, Client ID, and Client Secret are required for OAuth authorization.");
    }

    bool BeValidOAuthConfiguration(AddWebhook command)
    {
        if (!command.AuthorizationType.Equals("oauth", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(command.OAuthAuthority) &&
               !string.IsNullOrWhiteSpace(command.OAuthClientId) &&
               !string.IsNullOrWhiteSpace(command.OAuthClientSecret);
    }
}
