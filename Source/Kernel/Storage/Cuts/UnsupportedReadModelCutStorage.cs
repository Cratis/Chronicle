// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Cuts;

/// <summary>
/// Represents a fail-loud read-model cut storage for providers without cut storage support.
/// </summary>
public sealed class UnsupportedReadModelCutStorage : IReadModelCutStorage
{
    /// <summary>
    /// Gets the shared unsupported storage instance.
    /// </summary>
    public static readonly UnsupportedReadModelCutStorage Instance = new();

    /// <inheritdoc/>
    public Task SavePayload(ReadModelCutId id, ReadModelIdentifier readModel, string payloadJson) =>
        throw new ReadModelCutStorageNotSupported(nameof(SavePayload));

    /// <inheritdoc/>
    public Task<string?> GetPayload(ReadModelCutId id, ReadModelIdentifier readModel) =>
        throw new ReadModelCutStorageNotSupported(nameof(GetPayload));

    /// <inheritdoc/>
    public Task<bool> HasManifest(ReadModelCutId id) =>
        throw new ReadModelCutStorageNotSupported(nameof(HasManifest));

    /// <inheritdoc/>
    public Task<ReadModelCutManifest?> GetManifest(ReadModelCutId id) =>
        throw new ReadModelCutStorageNotSupported(nameof(GetManifest));

    /// <inheritdoc/>
    public Task PublishManifest(ReadModelCutManifest manifest) =>
        throw new ReadModelCutStorageNotSupported(nameof(PublishManifest));
}
