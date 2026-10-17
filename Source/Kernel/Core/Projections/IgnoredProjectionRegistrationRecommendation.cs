// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IIgnoredProjectionRegistrationRecommendation"/>.
/// </summary>
public class IgnoredProjectionRegistrationRecommendation : Recommendation<IgnoredProjectionRegistrationRequest>, IIgnoredProjectionRegistrationRecommendation;
