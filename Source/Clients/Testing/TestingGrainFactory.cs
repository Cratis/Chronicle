// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an <see cref="IGrainFactory"/> for top-level testing services.
/// </summary>
/// <remarks>
/// Individual grain lookups will throw <see cref="GrainNotAvailableInTestScenario"/> naming what was asked for
/// indicating which grain type was requested. As more in-process grain support is added to the testing
/// infrastructure, this factory can be extended to return them.
/// </remarks>
internal sealed class TestingGrainFactory : IGrainFactory
{
    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithGuidKey =>
        throw new GrainNotAvailableInTestScenario($"Grain '{typeof(TGrainInterface).FullName}' with a Guid key");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithIntegerKey =>
        throw new GrainNotAvailableInTestScenario($"Grain '{typeof(TGrainInterface).FullName}' with a integer key");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithStringKey =>
        throw new GrainNotAvailableInTestScenario($"Grain '{typeof(TGrainInterface).FullName}' with the string key '{primaryKey}'");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithGuidCompoundKey =>
        throw new GrainNotAvailableInTestScenario($"Grain '{typeof(TGrainInterface).FullName}' with a Guid compound key");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithIntegerCompoundKey =>
        throw new GrainNotAvailableInTestScenario($"Grain '{typeof(TGrainInterface).FullName}' with a integer compound key");

    /// <inheritdoc/>
    public TObjectInterface CreateObjectReference<TObjectInterface>(IGrainObserver obj)
        where TObjectInterface : IGrainObserver =>
        throw new GrainNotAvailableInTestScenario("A grain observer reference");

    /// <inheritdoc/>
    public void DeleteObjectReference<TObjectInterface>(IGrainObserver obj)
        where TObjectInterface : IGrainObserver =>
        throw new GrainNotAvailableInTestScenario("A grain observer reference");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId)
        where TGrainInterface : IAddressable =>
        throw new GrainNotAvailableInTestScenario($"Grain '{typeof(TGrainInterface).FullName}' with a GrainId");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey) =>
        throw new GrainNotAvailableInTestScenario($"Grain '{grainInterfaceType.FullName}'");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey) =>
        throw new GrainNotAvailableInTestScenario($"Grain '{grainInterfaceType.FullName}'");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey) =>
        throw new GrainNotAvailableInTestScenario($"Grain '{grainInterfaceType.FullName}'");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension) =>
        throw new GrainNotAvailableInTestScenario($"Grain '{grainInterfaceType.FullName}'");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension) =>
        throw new GrainNotAvailableInTestScenario($"Grain '{grainInterfaceType.FullName}'");

    /// <inheritdoc/>
    public IAddressable GetGrain(GrainId grainId) =>
        throw new GrainNotAvailableInTestScenario("A non-generic grain lookup");

    /// <inheritdoc/>
    public IAddressable GetGrain(GrainId grainId, GrainInterfaceType interfaceType) =>
        throw new GrainNotAvailableInTestScenario("A non-generic grain lookup");

    /// <inheritdoc/>
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey, string grainClassNamePrefix) =>
        throw new GrainNotAvailableInTestScenario($"Grain '{interfaceType.FullName}'");

    /// <inheritdoc/>
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey) =>
        throw new GrainNotAvailableInTestScenario($"Grain '{interfaceType.FullName}'");
}
