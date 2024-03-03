// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.AspNetCore.Identities;
using Cratis.Identities;

namespace Cratis;

/// <summary>
/// Extension methods for <see cref="IClientBuilder"/> for configuring the caused by identity provider.
/// </summary>
public static class IdentityProviderClientBuilderExtensions
{
    /// <summary>
    /// Use the <see cref="IdentityProvider"/> as the <see cref="IIdentityProvider"/>.
    /// </summary>
    /// <param name="builder"><see cref="IClientBuilder"/> to work with.</param>
    /// <returns><see cref="IClientBuilder"/> for continuation.</returns>
    public static IClientBuilder UseAspNetCoreIdentityProvider(this IClientBuilder builder) => builder.UseIdentityProvider<IdentityProvider>();
}
