// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents a key resolution that was deferred to a future.
/// </summary>
/// <param name="Future">The projection future.</param>
public record DeferredKey(ProjectionFuture Future) : KeyResolverResult;
