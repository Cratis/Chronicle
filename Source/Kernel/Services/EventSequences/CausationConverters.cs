// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;

namespace Aksio.Cratis.Kernel.Services.EventSequences;

/// <summary>
/// Extension methods for converting to and from <see cref="Causation"/>.
/// </summary>
public static class CausationConverters
{
    /// <summary>
    /// Convert to Kernel representation.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Contracts.Auditing.Causation"/> to convert from..</param>
    /// <returns>Converted collection of <see cref="Causation"/>.</returns>
    public static IEnumerable<Causation> ToKernel(this IEnumerable<Contracts.Auditing.Causation> causations) =>
        causations.Select(c => c.ToKernel());

    /// <summary>
    /// Convert to Kernel representation.
    /// </summary>
    /// <param name="causation"><see cref="Contracts.Auditing.Causation"/> to convert from.</param>
    /// <returns>Converted <see cref="Causation"/>.</returns>
    public static Causation ToKernel(this Contracts.Auditing.Causation causation) =>
        new(causation.Occurred, causation.Type, causation.Properties);
}
