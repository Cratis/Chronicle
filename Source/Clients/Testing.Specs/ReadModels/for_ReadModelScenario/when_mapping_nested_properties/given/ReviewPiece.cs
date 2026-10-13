// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// The compact metadata for choosing a draft to review.
/// </summary>
/// <param name="Id">The content identity.</param>
/// <param name="Topic">The topic.</param>
/// <param name="Author">The author.</param>
/// <param name="Revision">The revision.</param>
public record ReviewPiece(Guid Id, string Topic, string Author, int Revision);
