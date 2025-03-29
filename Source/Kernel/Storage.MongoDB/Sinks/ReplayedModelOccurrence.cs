// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an occurence of a replayed model.
/// </summary>
/// <param name="Occurred"><see cref="DateTimeOffset"/> for when it occurred.</param>
/// <param name="RevertModel">Name of the revert model.</param>
public record ReplayedModelOccurrence(DateTimeOffset Occurred, ModelName RevertModel);
