// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraints"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Constraints)]
public class Constraints : Grain<ConstraintsState>, IConstraints
{
    /// <inheritdoc/>
    public Task<ConstraintCheckResult> Check(EventSourceId eventSourceId, EventType eventType, JsonObject content)
    {
        throw new NotImplementedException();
    }
}
