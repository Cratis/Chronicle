// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections;

/// <summary>
/// A read model whose class-level removal names an event type Chronicle never discovered - a record that lost its
/// <c>[EventType]</c> in a refactor, one in an assembly the artifacts provider does not scan, or one outside the
/// explicit artifact subset a test host registers.
/// </summary>
/// <param name="Name">The name.</param>
[RemovedWith<UnregisteredEvent>]
public record ProjectionNamingAnUnregisteredEvent(string Name);
