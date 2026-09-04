// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Cuts;

/// <summary>
/// Defines a namespace-scoped store for read-model cut payloads and their published manifests.
/// </summary>
/// <remarks>
/// Payloads and the manifest are written in two phases: every payload first, the manifest only once every
/// payload for the request has been written and verified present. A manifest is never partially published -
/// <see cref="PublishManifest"/> is the single atomic step that makes a capture visible at all.
/// </remarks>
public interface IReadModelCutStorage
{
    /// <summary>
    /// Saves one read model's captured payload, content-addressed by the cut id and read model.
    /// </summary>
    /// <param name="id">The <see cref="ReadModelCutId"/> the payload belongs to.</param>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> the payload is for.</param>
    /// <param name="payloadJson">The canonical JSON payload.</param>
    /// <returns>Awaitable task.</returns>
    Task SavePayload(ReadModelCutId id, ReadModelIdentifier readModel, string payloadJson);

    /// <summary>
    /// Gets one read model's captured payload.
    /// </summary>
    /// <param name="id">The <see cref="ReadModelCutId"/> the payload belongs to.</param>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> the payload is for.</param>
    /// <returns>The payload JSON, or <see langword="null"/> when not found.</returns>
    Task<string?> GetPayload(ReadModelCutId id, ReadModelIdentifier readModel);

    /// <summary>
    /// Checks whether a manifest has already been published for a cut id.
    /// </summary>
    /// <param name="id">The <see cref="ReadModelCutId"/> to check.</param>
    /// <returns>True if a manifest exists; otherwise false.</returns>
    Task<bool> HasManifest(ReadModelCutId id);

    /// <summary>
    /// Gets the published manifest for a cut id.
    /// </summary>
    /// <param name="id">The <see cref="ReadModelCutId"/> to get the manifest for.</param>
    /// <returns>The <see cref="ReadModelCutManifest"/>, or <see langword="null"/> when not found.</returns>
    Task<ReadModelCutManifest?> GetManifest(ReadModelCutId id);

    /// <summary>
    /// Publishes a manifest, making the capture it describes visible.
    /// </summary>
    /// <param name="manifest">The <see cref="ReadModelCutManifest"/> to publish.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Idempotent - publishing the same id twice with identical content is a no-op; the deterministic id means a
    /// repeated identical request can safely publish again without creating a second record.
    /// </remarks>
    Task PublishManifest(ReadModelCutManifest manifest);
}
