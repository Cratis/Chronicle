// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Delegate for forwarding a changeset to a watching client's gRPC stream.
/// </summary>
/// <param name="namespaceName">The <see cref="EventStoreNamespaceName"/> the changeset belongs to.</param>
/// <param name="readModelKey">The <see cref="ReadModelKey"/> identifying the read model instance.</param>
/// <param name="readModel">The already-released read model as a <see cref="JsonObject"/>. The projection pipeline owns release; forwarders must not release it again.</param>
/// <param name="change">The <see cref="ReadModelChangeContext"/> describing the change.</param>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task ChangesetForwarder(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change);
