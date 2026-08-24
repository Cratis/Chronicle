// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents projection definitions that were rejected while the other definitions in the batch were registered.
/// </summary>
/// <param name="Failures">The failures keyed by projection identifier.</param>
[GenerateSerializer]
public record ProjectionRegistrationError(
    [property: Id(0)] IReadOnlyDictionary<ProjectionId, Exception> Failures);
