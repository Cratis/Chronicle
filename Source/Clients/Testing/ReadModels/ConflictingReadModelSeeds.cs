// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when a scalar seed and a named composite seed resolve to the same storage identity.
/// </summary>
/// <param name="readModelType">The seeded read model type.</param>
/// <param name="identity">The storage identity both seeds resolve to.</param>
public class ConflictingReadModelSeeds(Type readModelType, EventSourceId identity)
    : Exception($"Read model '{readModelType.FullName}' is seeded both by event source id and by named key parts for the same identity '{identity.Value}'. Seed each identity only once per scenario.");
