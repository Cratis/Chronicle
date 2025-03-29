// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Extension methods for converting to and from <see cref="Causation"/>.
/// </summary>
internal static class CausationConverters
{
    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Contracts.Auditing.Causation"/>.</returns>
    internal static IList<Contracts.Auditing.Causation> ToContract(this IEnumerable<Causation> causations) =>
        causations.Select(c => c.ToContract()).ToList();

    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="causation"><see cref="Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Auditing.Causation"/>.</returns>
    internal static Contracts.Auditing.Causation ToContract(this Causation causation) =>
        new()
        {
            Occurred = causation.Occurred!,
            Type = causation.Type,
            Properties = causation.Properties
        };

    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Contracts.Auditing.Causation"/> to convert from..</param>
    /// <returns>Converted collection of <see cref="Causation"/>.</returns>
    internal static IEnumerable<Causation> ToClient(this IEnumerable<Contracts.Auditing.Causation> causations) =>
        causations.Select(c => c.ToClient()).ToArray();

    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="causation"><see cref="Contracts.Auditing.Causation"/> to convert from.</param>
    /// <returns>Converted <see cref="Causation"/>.</returns>
    internal static Causation ToClient(this Contracts.Auditing.Causation causation) =>
        new(causation.Occurred, causation.Type, causation.Properties ?? new Dictionary<string, string>());
}
