// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when a read model is seeded twice for the same key in a scenario.
/// </summary>
/// <param name="readModelType">The seeded read model type.</param>
/// <param name="key">The key that was already seeded.</param>
public class ReadModelAlreadySeeded(Type readModelType, EventSourceId key)
    : Exception($"Read model '{readModelType.FullName}' is already seeded for key '{key.Value}'. Seed each key only once per scenario.");
