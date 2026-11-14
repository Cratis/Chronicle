// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// A secret under an earlier name that was tied to one address.
/// </summary>
/// <param name="Name">The <see cref="LegacySecretName"/>.</param>
/// <param name="Source">The earlier secret name.</param>
internal record LegacyEntry(LegacySecretName Name, string Source)
{
    /// <summary>
    /// Gets the name the secret is copied to.
    /// </summary>
    public string Destination => Name.Destination;
}
