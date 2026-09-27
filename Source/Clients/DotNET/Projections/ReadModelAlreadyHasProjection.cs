// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a projection is registered explicitly for a read model that another projection already maintains.
/// </summary>
/// <param name="readModelType">The read model type.</param>
/// <param name="existingProjectionId">The <see cref="ProjectionId"/> of the projection already maintaining the read model.</param>
/// <param name="projectionId">The <see cref="ProjectionId"/> of the projection being registered.</param>
public class ReadModelAlreadyHasProjection(Type readModelType, ProjectionId existingProjectionId, ProjectionId projectionId)
    : Exception($"Read model '{readModelType.FullName}' is already maintained by projection '{existingProjectionId}', so projection '{projectionId}' cannot be registered for it. A read model is maintained by one projection.");
