// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents the context for a replay.
/// </summary>
/// <param name="ReadModel">The name of the read model being replayed.</param>
/// <param name="RevertModel">The name of the model representing what one can revert / downgrade to.</param>
/// <param name="Started">The date and time for when the replay was started.</param>
/// <remarks>
/// The sink should replay to a temporary target, and then rename to the actual target when done. This would
/// prevent any changes to the target while replaying and keeping a system operational, at least from a read perspective.
/// Once replay is done it should keep the "old" target around by renaming it a the <see cref="RevertModel"/>.
/// </remarks>
public record ReplayContext(ReadModelName ReadModel, ReadModelName RevertModel, DateTimeOffset Started);
