// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
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
    readonly IWebhookTargetPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddWebhookValidator"/> class.
    /// </summary>
    /// <param name="registrar">The <see cref="WebhookRegistrar"/> to test endpoints and authorizations with.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the existing webhooks.</param>
    /// <param name="policy">The shared webhook and OAuth target policy.</param>
    public AddWebhookValidator(WebhookRegistrar registrar, IStorage storage, IWebhookTargetPolicy policy)
    {
        _registrar = registrar;
        _storage = storage;
        _policy = policy;

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
            .CustomAsync(ValidateTargets)
            .DependentRules(() =>
            {
                RuleFor(_ => _)
                    .MustAsync(BeValidOAuthConfiguration)
                    .WithMessage("Unable to acquire a valid OAuth token.");

                RuleFor(_ => _)
                    .MustAsync(BeValidWebhookEndpoint)
                    .WithMessage("Unable to connect to the webhook endpoint.");
            });

        RuleFor(_ => _)
            .MustAsync(NotHaveDuplicateName)
            .WithMessage("A webhook with the same name already exists.");
    }

    async Task ValidateTargets(AddWebhook command, ValidationContext<AddWebhook> context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(command.Url))
        {
            var target = await _policy.Evaluate(command.Url, command.EventStore, cancellationToken);
            if (!target.IsAllowed) context.AddFailure(nameof(command.Url), target.Reason);
        }

        if (command.AuthorizationType == AuthorizationType.OAuth && !string.IsNullOrEmpty(command.OAuthAuthority))
        {
            var authority = await _policy.Evaluate(command.OAuthAuthority, command.EventStore, cancellationToken);
            if (!authority.IsAllowed) context.AddFailure(nameof(command.OAuthAuthority), authority.Reason);
        }
    }

    async Task<bool> BeValidOAuthConfiguration(AddWebhook command, CancellationToken cancellationToken)
    {
        if (command.AuthorizationType != AuthorizationType.OAuth)
        {
            return true;
        }

        var result = await _registrar.TestOAuthAuthorization(command.OAuthAuthority, command.OAuthClientId, command.OAuthClientSecret, command.EventStore);
        return result.Success;
    }

    async Task<bool> BeValidWebhookEndpoint(AddWebhook command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.Url))
        {
            return true;
        }

        var result = await _registrar.TestWebhook(command.Target(), command.EventStore);
        return result.Success;
    }

    async Task<bool> NotHaveDuplicateName(AddWebhook command, CancellationToken cancellationToken)
    {
        var existing = await _storage.GetEventStore(command.EventStore).Webhooks.GetAll();
        return !existing.Any(_ => _.Identifier == command.Name);
    }
}
