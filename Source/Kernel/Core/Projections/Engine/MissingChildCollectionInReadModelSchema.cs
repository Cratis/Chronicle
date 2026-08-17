// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when a projection definition declares a child collection on a property
/// that does not exist in the read model schema - typically a stale projection definition that was kept
/// after the read model dropped the property.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingChildCollectionInReadModelSchema"/> class.
/// </remarks>
/// <param name="projectionId">The <see cref="ProjectionId"/> of the projection whose definition is inconsistent with the schema.</param>
/// <param name="childProperty">The property path the definition declares a child collection for.</param>
/// <param name="readModelIdentifier">The <see cref="ReadModelIdentifier"/> of the read model the projection targets.</param>
/// <param name="availableProperties">The properties the read model schema actually has.</param>
public class MissingChildCollectionInReadModelSchema(
    ProjectionId projectionId,
    string childProperty,
    ReadModelIdentifier readModelIdentifier,
    IEnumerable<string> availableProperties) : Exception(
        $"Projection '{projectionId}' declares a child collection at '{childProperty}', but the schema for read model " +
        $"'{readModelIdentifier}' has no such property (available properties: {string.Join(", ", availableProperties)}). " +
        "The stored projection definition is stale relative to the read model schema; re-registering the projection " +
        "with a definition that matches the read model resolves this.");
