// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Recommendations;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="RecommendationType"/> is encountered.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownClrTypeForRecommendationType"/> class.
/// </remarks>
/// <param name="type"><see cref="RecommendationType"/> that has an invalid type identifier.</param>
public class UnknownClrTypeForRecommendationType(RecommendationType type) : Exception($"Unknown operation type '{type}'")
{
}
