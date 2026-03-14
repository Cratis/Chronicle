// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Security;
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
        _webhooks = webhooks;

        When(_ => _.AuthorizationType == Security.AuthorizationType.OAuth, () =>
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

        RuleFor(_ => _)
            .MustAsync(BeValidWebhookEndpoint)
            .WithMessage("Unable to connect to the webhook endpoint.");

        RuleFor(_ => _)
            .MustAsync(NotHaveDuplicateName)
            .WithMessage("A webhook with the same name already exists.");
    }

    static OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? CreateAuthorization(AddWebhook command) =>
        command.AuthorizationType switch
        {
            Security.AuthorizationType.Basic => new(new BasicAuthorization
            {
                Username = command.BasicUsername ?? string.Empty,
                Password = command.BasicPassword ?? string.Empty
            }),
            Security.AuthorizationType.Bearer => new(new BearerTokenAuthorization
            {
                Token = command.BearerToken ?? string.Empty
            }),
            Security.AuthorizationType.OAuth => new(new OAuthAuthorization
            {
                Authority = command.OAuthAuthority ?? string.Empty,
                ClientId = command.OAuthClientId ?? string.Empty,
                ClientSecret = command.OAuthClientSecret ?? string.Empty
            }),
            _ => null
        };

    async Task<bool> BeValidOAuthConfiguration(AddWebhook command, CancellationToken cancellationToken)
    {
        if (command.AuthorizationType != Security.AuthorizationType.OAuth)
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

    async Task<bool> BeValidWebhookEndpoint(AddWebhook command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.Url))
        {
            return true;
        }

        var authorization = CreateAuthorization(command);
        var target = new WebhookTarget
        {
            Url = command.Url,
            Authorization = authorization,
            Headers = command.Headers.ToDictionary(h => h.Key, h => h.Value)
        };

        var result = await _webhooks.TestWebhook(
            new TestWebhookRequest { Target = target },
            cancellationToken);

        return result.Success;
    }

    async Task<bool> NotHaveDuplicateName(AddWebhook command, CancellationToken cancellationToken)
    {
        var existingWebhooks = await _webhooks.GetWebhooks(
            new GetWebhooksRequest
            {
                EventStore = command.EventStore
            });

        return !existingWebhooks.Any(_ => _.Identifier == command.Name);
    }
}
