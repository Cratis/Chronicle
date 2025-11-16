// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Users;
using Cratis.Chronicle.Storage.Users;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Extension methods for adding authentication to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Chronicle authentication services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChronicleAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IUserStorage, UserStorage>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        return services;
    }
}
