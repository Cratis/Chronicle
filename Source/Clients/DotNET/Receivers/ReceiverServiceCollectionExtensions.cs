// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Registers the transport-independent receiver.
/// </summary>
internal static class ReceiverServiceCollectionExtensions
{
    /// <summary>
    /// Adds the shared receiver services without replacing host overrides.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="options">Resolves the client options.</param>
    /// <returns>The service collection for further configuration.</returns>
    internal static IServiceCollection AddChronicleReceiver(this IServiceCollection services, Func<IServiceProvider, ChronicleOptions> options)
    {
        services.TryAddSingleton<IDeliveryDecoder>(provider => new WebhookBodyDecoder(options(provider).Serverless.Delivery));
        services.TryAddSingleton<DeliveryTargetValidation>();
        services.TryAddSingleton<IChronicleReceiver, ChronicleReceiver>();
        services.TryAddSingleton<IDeliveryStatusCodes, ProvisionalDeliveryStatusCodes>();
        return services;
    }
}
