// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Configuration;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IConfiguration"/>.
/// </summary>
public class Configuration : Grain, IConfiguration
{
    readonly IExecutionContextManager _executionContextManager;
    readonly KernelConfiguration _kernelConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="Configuration"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the current execution context.</param>
    /// <param name="kernelConfiguration"><see cref="Storage"/> configuration.</param>
    public Configuration(IExecutionContextManager executionContextManager, KernelConfiguration kernelConfiguration)
    {
        _executionContextManager = executionContextManager;
        _kernelConfiguration = kernelConfiguration;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TenantId>> GetTenants() => Task.FromResult(_kernelConfiguration.Tenants.GetTenantIds());

    /// <inheritdoc/>
    public Task<StorageForMicroservice> GetStorage() => Task.FromResult(_kernelConfiguration.Storage.Microservices.Get(_executionContextManager.Current.MicroserviceId));
}
