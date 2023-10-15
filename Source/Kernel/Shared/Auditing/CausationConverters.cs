// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Extension methods for converting to and from <see cref="Causation"/>.
/// </summary>
public static class CausationConverters
{
    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Kernel.Contracts.Auditing.Causation"/>.</returns>
    public static IEnumerable<Kernel.Contracts.Auditing.Causation> ToContract(this IEnumerable<Causation> causations) =>
        causations.Select(c => c.ToContract()).ToArray();

    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="causation"><see cref="Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Kernel.Contracts.Auditing.Causation"/>.</returns>
    public static Kernel.Contracts.Auditing.Causation ToContract(this Causation causation) =>
        new()
        {
            Occurred = causation.Occurred!,
            Type = causation.Type,
            Properties = causation.Properties
        };

    /// <summary>
    /// Convert to Kernel representation.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Kernel.Contracts.Auditing.Causation"/> to convert from..</param>
    /// <returns>Converted collection of <see cref="Causation"/>.</returns>
    public static IEnumerable<Causation> ToKernel(this IEnumerable<Kernel.Contracts.Auditing.Causation> causations) =>
        causations.Select(c => c.ToKernel()).ToArray();

    /// <summary>
    /// Convert to Kernel representation.
    /// </summary>
    /// <param name="causation"><see cref="Kernel.Contracts.Auditing.Causation"/> to convert from.</param>
    /// <returns>Converted <see cref="Causation"/>.</returns>
    public static Causation ToKernel(this Kernel.Contracts.Auditing.Causation causation) =>
        new(causation.Occurred, causation.Type, causation.Properties ?? new Dictionary<string, string>());
}
