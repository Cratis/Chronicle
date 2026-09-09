// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.DataProtection;
using OpenIddict.Validation;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions.given;

public class data_protection_access_tokens : chronicle_authentication_services
{
    protected const string Subject = "audience-regression-actor";
    protected SharedDataProtectionKeys _sharedKeys;
    protected ChronicleAuthenticationServices _issuer;
    protected ChronicleAuthenticationServices _validator;
    protected OpenIddictValidationService _validation;

    void Establish()
    {
        _sharedKeys = new();
        _issuer = BuildServices(_sharedKeys, authenticationEnabled: true, useInternalAuthority: true);
        _validator = BuildServices(_sharedKeys, authenticationEnabled: true, useInternalAuthority: true);
        _validation = _validator.ServiceProvider.GetRequiredService<OpenIddictValidationService>();
    }

    protected async Task<string> CreateToken(string? audience)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, Subject)], "Test"));
        principal.SetCreationDate(DateTimeOffset.UtcNow);
        principal.SetExpirationDate(DateTimeOffset.UtcNow.AddHours(1));
        principal.SetClaim(OpenIddictConstants.Claims.Private.Issuer, "https://localhost:35000/");
        principal.SetTokenType(OpenIddictConstants.TokenTypeIdentifiers.AccessToken);
        if (audience is not null) principal.SetAudiences(audience);

        await using var scope = _issuer.ServiceProvider.CreateAsyncScope();
        var transaction = await scope.ServiceProvider.GetRequiredService<IOpenIddictServerFactory>().CreateTransactionAsync();
        var context = new OpenIddictServerEvents.GenerateTokenContext(transaction)
        {
            Principal = principal,
            TokenType = OpenIddictConstants.TokenTypeIdentifiers.AccessToken
        };

        // Use OpenIddict's real Data Protection issuer, including its formatter and protection purposes.
        // No token entry or user record is needed for a self-contained access token.
        await scope.ServiceProvider.GetRequiredService<OpenIddictServerDataProtectionHandlers.Protection.GenerateDataProtectionToken>().HandleAsync(context);
        return context.Token!;
    }

    void Destroy()
    {
        _issuer.ServiceProvider.Dispose();
        _validator.ServiceProvider.Dispose();
    }
}
