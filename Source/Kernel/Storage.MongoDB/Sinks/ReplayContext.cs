// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the MongoDB version of a replay context.
/// </summary>
/// <param name="ModelName">The model name.</param>
/// <param name="RevertModelName">The revert model name.</param>
/// <param name="Started">The started date and time.</param>
public record ReplayContext(ReadModelName ModelName, ReadModelName RevertModelName, DateTimeOffset Started);
