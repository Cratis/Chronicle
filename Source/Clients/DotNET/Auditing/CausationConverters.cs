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
            Occurred = causation.Occurred,
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

    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Contracts.Sequences.Causation"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="Causation"/>.</returns>
    internal static IEnumerable<Causation> ToClient(this IEnumerable<Contracts.Sequences.Causation> causations) =>
        causations.Select(c => c.ToClient()).ToArray();

    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="causation"><see cref="Contracts.Sequences.Causation"/> to convert from.</param>
    /// <returns>Converted <see cref="Causation"/>.</returns>
    internal static Causation ToClient(this Contracts.Sequences.Causation causation) =>
        new(causation.Occurred, causation.Type, causation.Properties ?? new Dictionary<string, string>());

    /// <summary>
    /// Convert to the <see cref="Contracts.Sequences.Causation"/> contract representation.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Contracts.Sequences.Causation"/>.</returns>
    /// <remarks>
    /// Named distinctly from <see cref="ToContract(IEnumerable{Causation})"/> - both take a <see cref="Causation"/>
    /// receiver, so only the return type would tell them apart, and overload resolution cannot do that.
    /// </remarks>
    internal static IList<Contracts.Sequences.Causation> ToSequencesContract(this IEnumerable<Causation> causations) =>
        causations.Select(c => c.ToSequencesContract()).ToList();

    /// <summary>
    /// Convert to the <see cref="Contracts.Sequences.Causation"/> contract representation.
    /// </summary>
    /// <param name="causation"><see cref="Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Sequences.Causation"/>.</returns>
    internal static Contracts.Sequences.Causation ToSequencesContract(this Causation causation) =>
        new()
        {
            Occurred = causation.Occurred,
            Type = causation.Type,
            Properties = causation.Properties
        };
}
