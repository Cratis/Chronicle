// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an occurrence of a replayed read model.
/// </summary>
/// <param name="Occurred"><see cref="DateTimeOffset"/> for when it occurred.</param>
/// <param name="RevertModel">Name of the revert read model.</param>
public record ReplayedReadModelOccurrence(DateTimeOffset Occurred, ReadModelName RevertModel);
