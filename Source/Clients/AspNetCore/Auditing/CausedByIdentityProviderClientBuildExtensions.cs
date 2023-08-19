// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;

namespace Aksio.Cratis.AspNetCore.Auditing;

/// <summary>
/// Extension methods for <see cref="IClientBuilder"/> for configuring the caused by identity provider
/// </summary>
public static class CausedByIdentityProviderClientBuildExtensions
{
    /// <summary>
    /// Use the <see cref="CausedByIdentityProvider"/> as the <see cref="ICausedByIdentityProvider"/>.
    /// </summary>
    /// <param name="builder"><see cref="IClientBuilder"/> to work with.</param>
    /// <returns><see cref="IClientBuilder"/> for continuation.</returns>
    public static IClientBuilder UseAspNetCoreCausedByIdentityProvider(this IClientBuilder builder) => builder.UseCausedByIdentityProvider<CausedByIdentityProvider>();
}
