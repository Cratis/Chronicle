// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1600
namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for wiring up cluster-wide encryption key cache invalidation.
/// </summary>
public static class EncryptionKeyCacheInvalidationExtensions
{
    /// <summary>
    /// Add the per-silo grain service and its fan-out client used to evict encryption key caches across the cluster.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddEncryptionKeyCacheInvalidation(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddGrainService<EncryptionKeyCacheGrainService>();
        siloBuilder.ConfigureServices(_ => _.AddSingleton<IEncryptionKeyCacheClient, EncryptionKeyCacheClient>());
        return siloBuilder;
    }
}
