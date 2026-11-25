// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents a read model.
/// </summary>
/// <param name="Identifier">The identifier of the read model.</param>
/// <param name="Generation">The generation of the read model.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.ReadModels.ReadModelType")]
public record ReadModelType([Id(0)] ReadModelIdentifier Identifier, [Id(1)] ReadModelGeneration Generation);
