// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.Chronicle.Sequences;
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
    readonly IManagedIdentityTokens _managedIdentityTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddWebhookValidator"/> class.
    /// </summary>
    /// <param name="registrar">The <see cref="WebhookRegistrar"/> to test endpoints and authorizations with.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the existing webhooks.</param>
    /// <param name="policy">The shared webhook and OAuth target policy.</param>
    public AddWebhookValidator(WebhookRegistrar registrar, IStorage storage, IWebhookTargetPolicy policy)
        : this(registrar, storage, policy, new ManagedIdentityTokens())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddWebhookValidator"/> class.
    /// </summary>
    /// <param name="registrar">The registrar to test endpoints and authorizations with.</param>
    /// <param name="storage">The storage holding existing webhooks.</param>
    /// <param name="policy">The shared webhook and OAuth target policy.</param>
    /// <param name="managedIdentityTokens">The provider for managed identity tokens.</param>
    public AddWebhookValidator(WebhookRegistrar registrar, IStorage storage, IWebhookTargetPolicy policy, IManagedIdentityTokens managedIdentityTokens)
    {
        _registrar = registrar;
        _storage = storage;
        _policy = policy;
        _managedIdentityTokens = managedIdentityTokens;

        RuleFor(_ => _.EventStore).NotSystemEventStore();

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

        RuleFor(_ => _.ManagedIdentityScope)
            .NotEmpty()
            .WithMessage("Managed identity scope is required when using managed identity authorization.")
            .When(_ => _.AuthorizationType == AuthorizationType.ManagedIdentity);

        RuleFor(_ => _)
            .CustomAsync(ValidateTargets)
            .DependentRules(() =>
            {
                RuleFor(_ => _)
                    .MustAsync(BeValidOAuthConfiguration)
                    .WithMessage("Unable to acquire a valid OAuth token.");

                RuleFor(_ => _)
                    .MustAsync(BeValidManagedIdentityConfiguration)
                    .WithMessage("Unable to acquire a valid managed identity token.");

                RuleFor(_ => _)
                    .MustAsync(BeValidWebhookEndpoint)
                    .WithMessage("Unable to connect to the webhook endpoint.");
            });

        RuleFor(_ => _)
            .MustAsync(NotHaveDuplicateName)
            .WithMessage("A webhook with the same name already exists.");
    }

    WebhookTargetValidation CheckManagedIdentity(AddWebhook command) => command.AuthorizationType == AuthorizationType.ManagedIdentity
        ? _policy.CheckManagedIdentity(new(command.ManagedIdentityClientId, command.ManagedIdentityScope), command.Url, command.EventStore)
        : WebhookTargetValidation.Allowed;

    async Task ValidateTargets(AddWebhook command, ValidationContext<AddWebhook> context, CancellationToken cancellationToken)
    {
        var approval = CheckManagedIdentity(command);
        if (!approval.IsAllowed)
        {
            context.AddFailure(approval.Reason);
            return;
        }

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

        var result = await _registrar.TestOAuthAuthorization(command.OAuthAuthority, command.OAuthClientId, command.OAuthClientSecret, command.OAuthScope, command.EventStore);
        return result.Success;
    }

    async Task<bool> BeValidManagedIdentityConfiguration(AddWebhook command, CancellationToken cancellationToken)
    {
        if (command.AuthorizationType != AuthorizationType.ManagedIdentity || string.IsNullOrWhiteSpace(command.ManagedIdentityScope))
        {
            return true;
        }

        try
        {
            var authorization = new Concepts.Security.ManagedIdentityAuthorization(command.ManagedIdentityClientId, command.ManagedIdentityScope);
            var token = await _managedIdentityTokens.AcquireToken(authorization, cancellationToken);
            return !string.IsNullOrEmpty(token.AccessToken) && !token.IsExpired;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
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
