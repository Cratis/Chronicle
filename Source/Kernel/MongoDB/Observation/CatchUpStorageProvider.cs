// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents a <see cref="ObserverStorageProvider"/> for handling catch-up observer state storage.
/// </summary>
public class CatchUpStorageProvider : ObserverStorageProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUpStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public CatchUpStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider) : base(executionContextManager, eventStoreDatabaseProvider)
    {
    }

    /// <inheritdoc/>
    public override Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        return Task.CompletedTask;
    }
}
