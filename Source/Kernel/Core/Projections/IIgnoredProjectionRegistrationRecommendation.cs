// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the grain that represents a projection registration that was ignored for being older than the stored one.
/// </summary>
/// <remarks>
/// There is nothing to perform: the recommendation tells an operator that an older client is still running. Ignoring or performing it dismisses it.
/// </remarks>
public interface IIgnoredProjectionRegistrationRecommendation : IRecommendation<IgnoredProjectionRegistrationRequest>;
