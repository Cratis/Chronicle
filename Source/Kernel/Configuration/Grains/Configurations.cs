// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Configuration.Grains;

/// <summary>
/// Defines a system for working with configurations.
/// </summary>
public class Configurations : Grain, IConfigurations
{
    readonly IExecutionContextManager _executionContextManager;
    readonly Storage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Configurations"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the current execution context.</param>
    /// <param name="storage"><see cref="Storage"/> configuration.</param>
    public Configurations(IExecutionContextManager executionContextManager, Storage storage)
    {
        _executionContextManager = executionContextManager;
        _storage = storage;
    }

    /// <summary>
    /// Gets the <see cref="Storage"/> configuration.
    /// </summary>
    /// <returns><see cref="Storage"/> configuration instance.</returns>
    public Task<StorageForMicroservice> GetStorage() => Task.FromResult(_storage.Get(_executionContextManager.Current.MicroserviceId));
}
