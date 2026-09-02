// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents a validator for <see cref="AddWebhook"/>.
/// </summary>
/// <remarks>
/// A webhook that cannot be reached, or whose OAuth settings do not yield a token, is a mistake to report back to
/// whoever is filling the form in - not a webhook to register and let fail silently on the first event.
/// </remarks>
public class AddWebhookValidator : CommandValidator<AddWebhook>
{
    readonly WebhookRegistrar _registrar;
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddWebhookValidator"/> class.
    /// </summary>
    /// <param name="registrar">The <see cref="WebhookRegistrar"/> to test endpoints and authorizations with.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the existing webhooks.</param>
    public AddWebhookValidator(WebhookRegistrar registrar, IStorage storage)
    {
        _registrar = registrar;
        _storage = storage;

        When(_ => _.AuthorizationType == AuthorizationType.OAuth, () =>
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

    async Task<bool> BeValidOAuthConfiguration(AddWebhook command, CancellationToken cancellationToken)
    {
        if (command.AuthorizationType != AuthorizationType.OAuth)
        {
            return true;
        }

        var result = await _registrar.TestOAuthAuthorization(command.OAuthAuthority, command.OAuthClientId, command.OAuthClientSecret);
        return result.Success;
    }

    async Task<bool> BeValidWebhookEndpoint(AddWebhook command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.Url))
        {
            return true;
        }

        var result = await _registrar.TestWebhook(command.Target());
        return result.Success;
    }

    async Task<bool> NotHaveDuplicateName(AddWebhook command, CancellationToken cancellationToken)
    {
        var existing = await _storage.GetEventStore(command.EventStore).Webhooks.GetAll();
        return !existing.Any(_ => _.Identifier == command.Name);
    }
}
