// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an <see cref="IGrainFactory"/> for top-level testing services.
/// </summary>
/// <remarks>
/// Individual grain lookups will throw <see cref="NotSupportedException"/> with descriptive messages
/// indicating which grain type was requested. As more in-process grain support is added to the testing
/// infrastructure, this factory can be extended to return them.
/// </remarks>
internal sealed class TestingGrainFactory : IGrainFactory
{
    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithGuidKey =>
        throw new NotSupportedException($"Grain '{typeof(TGrainInterface).FullName}' with Guid key is not available in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithIntegerKey =>
        throw new NotSupportedException($"Grain '{typeof(TGrainInterface).FullName}' with integer key is not available in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithStringKey =>
        throw new NotSupportedException($"Grain '{typeof(TGrainInterface).FullName}' with string key '{primaryKey}' is not available in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithGuidCompoundKey =>
        throw new NotSupportedException($"Grain '{typeof(TGrainInterface).FullName}' with Guid compound key is not available in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithIntegerCompoundKey =>
        throw new NotSupportedException($"Grain '{typeof(TGrainInterface).FullName}' with integer compound key is not available in test scenarios.");

    /// <inheritdoc/>
    public TObjectInterface CreateObjectReference<TObjectInterface>(IGrainObserver obj)
        where TObjectInterface : IGrainObserver =>
        throw new NotSupportedException("Grain observer references are not available in test scenarios.");

    /// <inheritdoc/>
    public void DeleteObjectReference<TObjectInterface>(IGrainObserver obj)
        where TObjectInterface : IGrainObserver =>
        throw new NotSupportedException("Grain observer references are not available in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId)
        where TGrainInterface : IAddressable =>
        throw new NotSupportedException($"Grain '{typeof(TGrainInterface).FullName}' with GrainId is not available in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey) =>
        throw new NotSupportedException($"Grain '{grainInterfaceType.FullName}' is not available in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey) =>
        throw new NotSupportedException($"Grain '{grainInterfaceType.FullName}' is not available in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey) =>
        throw new NotSupportedException($"Grain '{grainInterfaceType.FullName}' is not available in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension) =>
        throw new NotSupportedException($"Grain '{grainInterfaceType.FullName}' is not available in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension) =>
        throw new NotSupportedException($"Grain '{grainInterfaceType.FullName}' is not available in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(GrainId grainId) =>
        throw new NotSupportedException("Non-generic grain lookup is not available in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(GrainId grainId, GrainInterfaceType interfaceType) =>
        throw new NotSupportedException("Non-generic grain lookup is not available in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey, string grainClassNamePrefix) =>
        throw new NotSupportedException($"Grain '{interfaceType.FullName}' is not available in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey) =>
        throw new NotSupportedException($"Grain '{interfaceType.FullName}' is not available in test scenarios.");
}
