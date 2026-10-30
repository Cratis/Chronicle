// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;
using Azure.Identity;
using Cratis.Chronicle.Concepts.Security;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Server.Observation.Webhooks;

/// <summary>
/// Chooses workload identity when federation is configured, otherwise managed identity.
/// </summary>
/// <param name="environment">Reads identity environment settings.</param>
[Singleton]
public class ManagedIdentityCredentialFactory(Func<string, string?>? environment = null) : IManagedIdentityCredentialFactory
{
    readonly Func<string, string?> _environment = environment ?? Environment.GetEnvironmentVariable;

    /// <inheritdoc/>
    public TokenCredential Create(ManagedIdentityClientId clientId)
    {
        var tokenFile = _environment("AZURE_FEDERATED_TOKEN_FILE");
        if (!string.IsNullOrEmpty(tokenFile))
        {
            return new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
            {
                TokenFilePath = tokenFile,
                TenantId = _environment("AZURE_TENANT_ID"),
                ClientId = string.IsNullOrEmpty(clientId.Value) ? _environment("AZURE_CLIENT_ID") : clientId.Value
            });
        }

        return new ManagedIdentityCredential(string.IsNullOrEmpty(clientId.Value)
            ? ManagedIdentityId.SystemAssigned
            : ManagedIdentityId.FromUserAssignedClientId(clientId.Value));
    }
}
