// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using Cratis.Chronicle.Aggregates;
using Cratis.Reflection;
using Orleans.Storage;

namespace Cratis.Chronicle.Orleans.Aggregates;

public class AggregateRoot : Grain, IAggregateRoot
{
}


public class AggregateRoot<TState> : Grain<TState>, IAggregateRoot
{
}


public interface IAggregateRoot : IGrainWithStringKey, Client.Cratis.Chronicle.Aggregates.IAggregateRoot
{
}

public class AggregateRootStorageProvider : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => throw new NotImplementedException();
}


public class AggregateRootFactory : IAggregateRootFactory
{
    /// <inheritdoc/>
    public Task<T> Get<T>(EventSourceId id, bool autoCommit = true) where T : IAggregateRoot
    {
        AggregateRootMustBeGrainException.ThrowIfNotGrain(typeof(T));
    }
}


public class AggregateRootMustBeGrainException : Exception
{
    public AggregateRootMustBeGrainException(Type type) : base($"Aggregate root {type.FullName} must be a grain")
    {
    }

    public static void ThrowIfNotGrain(Type type)
    {
        if (!type.HasInterface<IGrain>())
        {
            throw new AggregateRootMustBeGrainException(type);
        }
    }
}
