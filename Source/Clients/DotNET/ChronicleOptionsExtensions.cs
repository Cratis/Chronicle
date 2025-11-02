// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle;

/// <summary>
/// Extensions for <see cref="ChronicleOptions"/>.
/// </summary>
public static class ChronicleOptionsExtensions
{
    /// <summary>
    /// Sets the <see cref="INamingPolicy"/> to use CamelCase naming.
    /// </summary>
    /// <param name="options"><see cref="ChronicleOptions"/> to set the naming policy on.</param>
    /// <returns>The same <see cref="ChronicleOptions"/> instance with the CamelCase naming policy set.</returns>
    public static ChronicleOptions WithCamelCaseNamingPolicy(this ChronicleOptions options)
    {
        options.NamingPolicy = new CamelCaseNamingPolicy();
        return options;
    }

    /// <summary>
    /// Configures the namespace resolver to use claims-based resolution from the current user principal.
    /// </summary>
    /// <param name="options"><see cref="ChronicleOptions"/> to configure.</param>
    /// <param name="claimType">The claim type to use for resolving the namespace. Defaults to "tenant_id".</param>
    /// <returns>The same <see cref="ChronicleOptions"/> instance with the claims-based namespace resolver configured.</returns>
    public static ChronicleOptions WithClaimsBasedNamespaceResolver(this ChronicleOptions options, string claimType = "tenant_id")
    {
        options.EventStoreNamespaceResolver = new ClaimsBasedNamespaceResolver(claimType);
        return options;
    }
}
