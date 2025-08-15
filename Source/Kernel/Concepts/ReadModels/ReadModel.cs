// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents a read model.
/// </summary>
/// <param name="Name">The name of the read model.</param>
/// <param name="Generation">The generation of the read model.</param>
public record ReadModel(ReadModelName Name, ReadModelGeneration Generation);
