// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an <see cref="IFencedBatchProbe"/> that does nothing.
/// </summary>
public sealed class NullFencedBatchProbe : IFencedBatchProbe
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static readonly NullFencedBatchProbe Instance = new();

    /// <inheritdoc/>
    public Task BeforeAdmission(FenceBatchId batchId) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task AfterAdmission(FenceBatchId batchId) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task BeforeCommit(FenceBatchId batchId) => Task.CompletedTask;
}
