// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac.Builder;
using Autofac.Core;

namespace Aksio.Cratis.Extensions.Autofac;

/// <summary>
/// Extension methods for working with <see cref="IRegistrationBuilder{T1,T2,T3}"/>.
/// </summary>
public static class RegistrationBuilderExtensions
{
    /// <summary>
    /// Share one instance of the component per tenant.
    /// </summary>
    /// <param name="registration">Registration to configure.</param>
    /// <typeparam name="TLimit">Limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Style type.</typeparam>
    /// <returns>Registration for continuation.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InstancePerTenant<TLimit, TActivatorData, TStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TStyle> registration)
    {
        ArgumentNullException.ThrowIfNull(registration, nameof(registration));
        registration.RegistrationData.Sharing = InstanceSharing.Shared;
        registration.RegistrationData.Lifetime = SingletonPerTenantComponentLifetime.Instance;

        return registration;
    }

    /// <summary>
    /// Share one instance of the component per microservice.
    /// </summary>
    /// <param name="registration">Registration to configure.</param>
    /// <typeparam name="TLimit">Limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Style type.</typeparam>
    /// <returns>Registration for continuation.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InstancePerMicroservice<TLimit, TActivatorData, TStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TStyle> registration)
    {
        ArgumentNullException.ThrowIfNull(registration, nameof(registration));
        registration.RegistrationData.Sharing = InstanceSharing.Shared;
        registration.RegistrationData.Lifetime = SingletonPerMicroserviceComponentLifetime.Instance;

        return registration;
    }

    /// <summary>
    /// Share one instance of the component per microservice and tenant.
    /// </summary>
    /// <param name="registration">Registration to configure.</param>
    /// <typeparam name="TLimit">Limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Style type.</typeparam>
    /// <returns>Registration for continuation.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InstancePerMicroserviceAndTenant<TLimit, TActivatorData, TStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TStyle> registration)
    {
        ArgumentNullException.ThrowIfNull(registration, nameof(registration));
        registration.RegistrationData.Sharing = InstanceSharing.Shared;
        registration.RegistrationData.Lifetime = SingletonPerMicroserviceAndTenantComponentLifetime.Instance;

        return registration;
    }
}
