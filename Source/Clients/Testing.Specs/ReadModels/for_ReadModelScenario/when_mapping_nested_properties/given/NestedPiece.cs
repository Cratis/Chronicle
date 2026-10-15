// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// Content with nested metadata.
/// </summary>
/// <param name="Id">The content identity.</param>
/// <param name="Meta">The metadata.</param>
/// <param name="Revision">The revision.</param>
public record NestedPiece(Guid Id, PieceMetadata Meta, int Revision);
