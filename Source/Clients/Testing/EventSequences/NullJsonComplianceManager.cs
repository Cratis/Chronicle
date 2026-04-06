// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using KernelCore::Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a pass-through implementation of <see cref="IJsonComplianceManager"/> for in-process testing.
/// </summary>
/// <remarks>
/// Returns JSON content unchanged. No compliance rules (e.g. PII masking) are applied during testing.
/// </remarks>
internal sealed class NullJsonComplianceManager : IJsonComplianceManager
{
    /// <inheritdoc/>
    public Task<JsonObject> Apply(
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore,
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        string identifier,
        JsonObject json) =>
        Task.FromResult(json);

    /// <inheritdoc/>
    public Task<JsonObject> Release(
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore,
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        string identifier,
        JsonObject json) =>
        Task.FromResult(json);
}
