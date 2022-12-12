// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Applications.Identity;
using Aksio.Cratis.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for setting up identity providers.
/// </summary>
public static class IdentityProviderEndpointExtensions
{
    const string PrincipalHeader = "x-ms-client-principal";
    const string IdentityIdHeader = "x-ms-client-principal-id";
    const string IdentityNameHeader = "x-ms-client-principal-name";

    /// <summary>
    /// Map identity provider endpoints.
    /// </summary>
    /// <param name="endpoints">Endpoints to extend.</param>
    /// <param name="app"><see cref="IApplicationBuilder"/> adding to.</param>
    /// <returns>Continuation.</returns>
    public static IEndpointRouteBuilder MapIdentityProvider(this IEndpointRouteBuilder endpoints, IApplicationBuilder app)
    {
        var serializerOptions = app.ApplicationServices.GetService<JsonSerializerOptions>()!;
        var types = app.ApplicationServices.GetService<ITypes>()!;
        var providerTypes = types.FindMultiple<IProvideIdentityDetails>().ToArray();
        if (providerTypes.Length > 1)
        {
            throw new MultipleIdentityDetailsProvidersFound(providerTypes);
        }

        if (providerTypes.Length == 1)
        {
            var provider = (app.ApplicationServices.GetService(providerTypes[0]) as IProvideIdentityDetails)!;
            endpoints.MapGet(".aksio/me", async (HttpRequest request, HttpResponse response) =>
            {
                if (request.Headers.ContainsKey(IdentityIdHeader) &&
                    request.Headers.ContainsKey(IdentityNameHeader) &&
                    request.Headers.ContainsKey(PrincipalHeader))
                {
                    IdentityId identityId = request.Headers[IdentityIdHeader].ToString();
                    IdentityName identityName = request.Headers[IdentityNameHeader].ToString();
                    var token = Convert.FromBase64String(request.Headers[PrincipalHeader]);
                    var decodedToken = Encoding.Default.GetString(token);

                    var tokenAsJson = JsonNode.Parse(token) as JsonObject;
                    var claims = new List<KeyValuePair<string, string>>();
                    if (tokenAsJson is not null && tokenAsJson.TryGetPropertyValue("claims", out var claimsArray) && claimsArray is JsonArray claimsAsArray)
                    {
                        foreach (var claim in claimsAsArray.Cast<JsonObject>())
                        {
                            if (claim.TryGetPropertyValue("typ", out var type))
                            {
                                if (claim.TryGetPropertyValue("val", out var value))
                                {
                                    claims.Add(new KeyValuePair<string, string>(type!.ToString(), value!.ToString()));
                                }
                            }
                        }

                        var context = new IdentityProviderContext(identityId, identityName, tokenAsJson, claims);
                        var result = await provider.Provide(context);
                        response.StatusCode = 200;
                        await response.WriteAsJsonAsync<object>(result, serializerOptions);
                    }
                }
            });
        }

        return endpoints;
    }
}
