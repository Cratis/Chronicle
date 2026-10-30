// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Server.Observation.Webhooks;

/// <summary>
/// Creates an Azure credential for a managed or workload identity.
/// </summary>
public interface IManagedIdentityCredentialFactory
{
    /// <summary>
    /// Creates a credential without attempting token acquisition.
    /// </summary>
    /// <param name="clientId">The client identifier, empty for the system-assigned identity.</param>
    /// <returns>The Azure credential.</returns>
    TokenCredential Create(ManagedIdentityClientId clientId);
}
