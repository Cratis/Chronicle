// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Recommendations;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="RecommendationType"/> is encountered.
/// </summary>
public class UnknownClrTypeForRecommendationType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownClrTypeForRecommendationType"/> class.
    /// </summary>
    /// <param name="type"><see cref="RecommendationType"/> that has an invalid type identifier.</param>
    public UnknownClrTypeForRecommendationType(RecommendationType type)
        : base($"Unknown operation type '{type}'")
    {
    }
}
