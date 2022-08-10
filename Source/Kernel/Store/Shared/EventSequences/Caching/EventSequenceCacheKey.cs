// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Represents the cache key representing a unique instance of <see cref="EventSequenceCache"/>.
/// </summary>
/// <param name="EventSequenceId"><see cref="EventSequenceId"/> the cache is for.</param>
/// <param name="MicroserviceId"><see cref="MicroserviceId"/> the cache is for.</param>
/// <param name="TenantId"><see cref="TenantId"/> the cache is for.</param>
public record EventSequenceCacheKey(EventSequenceId EventSequenceId, MicroserviceId MicroserviceId, TenantId TenantId);
