// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// An abstract twin of <see cref="IGenerationWriterGrants"/>, whose members are default-implemented. The pinned
/// Castle.Core cannot intercept default interface members, so specifications substitute this and route through
/// <see cref="ForwardingWriterGrants"/>.
/// </summary>
public interface IWriterGrants
{
    Task<GenerationWriterGrant?> TryAcquire(EventStoreName store, GenerationObserverBinding binding, GenerationBuildOperationId attempt, long expectedEpoch);

    Task<GenerationWriterGrant?> GetAdmitted(EventStoreName store, GenerationObserverBinding binding);
}

/// <summary>
/// Forwards <see cref="IGenerationWriterGrants"/> to a substituted <see cref="IWriterGrants"/>.
/// </summary>
/// <param name="inner">The substituted grants.</param>
public class ForwardingWriterGrants(IWriterGrants inner) : IGenerationWriterGrants
{
    public Task<GenerationWriterGrant?> TryAcquire(EventStoreName store, GenerationObserverBinding binding, GenerationBuildOperationId attempt, long expectedEpoch) =>
        inner.TryAcquire(store, binding, attempt, expectedEpoch);

    public Task<GenerationWriterGrant?> GetAdmitted(EventStoreName store, GenerationObserverBinding binding) =>
        inner.GetAdmitted(store, binding);
}
