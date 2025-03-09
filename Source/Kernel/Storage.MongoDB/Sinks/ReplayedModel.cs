// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a replayed model.
/// </summary>
/// <param name="Model">Model that was replayed.</param>
/// <param name="Occurrences">Collection of <see cref="ReplayedModelOccurrence"/>.</param>
public record ReplayedModel(ModelName Model, IEnumerable<ReplayedModelOccurrence> Occurrences);
