// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using KernelEventSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a fake <see cref="IGrainFactory"/> that returns the in-process kernel
/// <see cref="KernelEventSequences::EventSequence"/> grain for any <see cref="KernelEventSequences::IEventSequence"/> lookup.
/// </summary>
/// <remarks>
/// All grain factory methods other than <c>GetGrain&lt;TGrainInterface&gt;(string, string?)</c> throw
/// <see cref="NotSupportedException"/>, since only event-sequence grain lookups are needed in test scenarios.
/// </remarks>
/// <param name="grain">The kernel <see cref="KernelEventSequences::EventSequence"/> grain to return.</param>
internal sealed class InProcessGrainFactory(KernelEventSequences::EventSequence grain) : IGrainFactory
{
    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithGuidKey =>
        throw new NotSupportedException("Guid-keyed grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithIntegerKey =>
        throw new NotSupportedException("Integer-keyed grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithStringKey
    {
        if (grain is TGrainInterface result)
        {
            return result;
        }

        throw new NotSupportedException($"Grain interface '{typeof(TGrainInterface).FullName}' is not supported in test scenarios.");
    }

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithGuidCompoundKey =>
        throw new NotSupportedException("Guid compound-keyed grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithIntegerCompoundKey =>
        throw new NotSupportedException("Integer compound-keyed grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public TObjectInterface CreateObjectReference<TObjectInterface>(IGrainObserver obj)
        where TObjectInterface : IGrainObserver =>
        throw new NotSupportedException("Grain observer references are not supported in test scenarios.");

    /// <inheritdoc/>
    public void DeleteObjectReference<TObjectInterface>(IGrainObserver obj)
        where TObjectInterface : IGrainObserver =>
        throw new NotSupportedException("Grain observer references are not supported in test scenarios.");

    /// <inheritdoc/>
    public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId)
        where TGrainInterface : IAddressable =>
        throw new NotSupportedException("GrainId-keyed grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(GrainId grainId) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(GrainId grainId, GrainInterfaceType interfaceType) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey, string grainClassNamePrefix) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");

    /// <inheritdoc/>
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey) =>
        throw new NotSupportedException("Non-generic grain lookup is not supported in test scenarios.");
}
