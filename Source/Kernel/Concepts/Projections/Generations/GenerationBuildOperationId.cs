// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Identifies a build operation within a store incarnation and namespace.
/// </summary>
/// <param name="Value">The operation identity.</param>
public sealed record GenerationBuildOperationId(Guid Value) : ConceptAs<Guid>(Value);
