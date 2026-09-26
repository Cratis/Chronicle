// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>A concurrency conflict for a protected read, without exposing sequence numbers.</summary>
/// <param name="ReadModelType">The read model that changed.</param>
/// <param name="Key">The key that changed.</param>
public record DecisionConflict(Type ReadModelType, ReadModelKey Key);
