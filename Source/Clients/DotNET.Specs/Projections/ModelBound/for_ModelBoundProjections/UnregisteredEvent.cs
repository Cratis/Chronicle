// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections;

/// <summary>
/// Deliberately carries no <c>[EventType]</c>, which is what makes it unresolvable. Model-bound projection
/// attributes take an unconstrained generic type argument, so naming this type in one compiles and passes every
/// analyzer.
/// </summary>
/// <param name="Name">The name.</param>
public record UnregisteredEvent(string Name);
