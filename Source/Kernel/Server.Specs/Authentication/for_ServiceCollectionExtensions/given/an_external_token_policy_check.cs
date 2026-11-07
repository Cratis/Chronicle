// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions.given;

/// <summary>
/// Validates a real JWT with the production JwtBearer parameters and runs the real Administrator policy.
/// </summary>
public class an_external_token_policy_check : chronicle_authentication_services
{
    protected bool _allowed;

    protected async Task<bool> Authorize(params string[] roles)
    {
        var mapping = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" };
        var authenticationServices = BuildServicesWithExternalAdministratorClaim(
            mapping,
            services =>
            {
                services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions
                {
                    Authentication = new Configuration.Authentication { Authority = "https://identity.example", ExternalAdministratorClaim = mapping }
                }));
                services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
            });
        await using var provider = authenticationServices.ServiceProvider;
        await using var scope = provider.CreateAsyncScope();

        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("spec-only-signing-key-0123456789-abcdef"));
        var parameters = jwtOptions.TokenValidationParameters.Clone();
        parameters.IssuerSigningKey = key;
        parameters.ValidIssuer = "https://identity.example";

        var handler = new JsonWebTokenHandler { MapInboundClaims = jwtOptions.MapInboundClaims };
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "https://identity.example",
            Audience = "chronicle",
            Claims = new Dictionary<string, object> { ["roles"] = roles },
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
        var validation = await handler.ValidateTokenAsync(token, parameters);
        validation.IsValid.ShouldBeTrue();

        // The policy runs for a request, as it does in the server: the administrator check comes from the request's services.
        var principal = new ClaimsPrincipal(validation.ClaimsIdentity);
        var request = new DefaultHttpContext { User = principal, RequestServices = scope.ServiceProvider };
        return (await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, request, WellKnownAuthorizationPolicies.Administrator)).Succeeded;
    }
}
