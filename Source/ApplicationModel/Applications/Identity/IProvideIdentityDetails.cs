// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Identity;

/// <summary>
/// Defines the system that provides identity details.
/// </summary>
public interface IProvideIdentityDetails
{
    /// <summary>
    /// Provide the details.
    /// </summary>
    /// <param name="context">The <see cref="IdentityProviderContext"/>.</param>
    /// <returns>The details.</returns>
    /// <remarks>
    /// The result of this will end up being serialized as JSON.
    /// </remarks>
    Task<object> Provide(IdentityProviderContext context);
}
