// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Security;
using FluentValidation;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the validator for <see cref="AddExternalService"/>.
/// </summary>
/// <remarks>
/// The command carries the fields of every endpoint and authorization kind at once, and which of them the
/// service actually stores is decided by <see cref="AddExternalService.EndpointType"/> and
/// <see cref="AddExternalService.AuthorizationType"/> - so each field is required only for the kind that
/// consumes it. Requiring them unconditionally would reject a perfectly valid HTTP service for leaving the
/// database fields blank. <see cref="AddExternalService.Id"/> is deliberately not required: the handler
/// falls back to the name when it is empty.
/// </remarks>
internal class AddExternalServiceValidator : CommandValidator<AddExternalService>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddExternalServiceValidator"/> class.
    /// </summary>
    public AddExternalServiceValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Name).NotEmpty().WithMessage("External service name is required.");

        When(_ => _.EndpointType == ExternalServiceEndpointType.Http, () =>
            RuleFor(_ => _.Url).NotEmpty().WithMessage("Url is required for an HTTP endpoint."));

        When(_ => _.EndpointType != ExternalServiceEndpointType.Http, () =>
        {
            RuleFor(_ => _.Host).NotEmpty().WithMessage("Host is required for a database endpoint.");
            RuleFor(_ => _.Database).NotEmpty().WithMessage("Database is required for a database endpoint.");
        });

        When(_ => _.AuthorizationType == AuthorizationType.Basic, () =>
        {
            RuleFor(_ => _.BasicUsername).NotEmpty().WithMessage("Username is required when using basic authorization.");
            RuleFor(_ => _.BasicPassword).NotEmpty().WithMessage("Password is required when using basic authorization.");
        });

        When(_ => _.AuthorizationType == AuthorizationType.Bearer, () =>
            RuleFor(_ => _.BearerToken).NotEmpty().WithMessage("Token is required when using bearer authorization."));

        When(_ => _.AuthorizationType == AuthorizationType.OAuth, () =>
        {
            RuleFor(_ => _.OAuthAuthority).NotEmpty().WithMessage("OAuth Authority is required when using OAuth authorization.");
            RuleFor(_ => _.OAuthClientId).NotEmpty().WithMessage("OAuth Client ID is required when using OAuth authorization.");
            RuleFor(_ => _.OAuthClientSecret).NotEmpty().WithMessage("OAuth Client Secret is required when using OAuth authorization.");
        });
    }
}
