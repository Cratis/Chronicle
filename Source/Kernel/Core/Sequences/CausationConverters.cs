// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts between <see cref="Causation"/> and its contract and storage representations.
/// </summary>
internal static class CausationConverters
{
    /// <summary>
    /// Converts an API causation to a contract causation.
    /// </summary>
    /// <param name="causation"><see cref="Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Sequences.Causation"/>.</returns>
    public static Contracts.Sequences.Causation ToContract(this Causation causation) => new()
    {
        Occurred = causation.Occurred,
        Type = causation.Type,
        Properties = causation.Properties.ToDictionary(x => x.Key, x => x.Value)
    };

    /// <summary>
    /// Converts a collection of API causations to contract causations.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Contracts.Sequences.Causation"/>.</returns>
    public static IList<Contracts.Sequences.Causation> ToContract(this IEnumerable<Causation> causations) =>
        causations.Select(c => c.ToContract()).ToList();

    /// <summary>
    /// Converts a storage causation to an API causation.
    /// </summary>
    /// <param name="causation"><see cref="Concepts.Auditing.Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Causation"/>.</returns>
    public static Causation ToApi(this Concepts.Auditing.Causation causation) => new(
            causation.Occurred,
            causation.Type,
            causation.Properties.ToDictionary(x => x.Key, x => x.Value));

    /// <summary>
    /// Converts a collection of storage causations to API causations.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Concepts.Auditing.Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Causation"/>.</returns>
    public static IEnumerable<Causation> ToApi(this IEnumerable<Concepts.Auditing.Causation> causations) =>
        causations.Select(c => c.ToApi()).ToArray();

    /// <summary>
    /// Converts a contract causation to an API causation.
    /// </summary>
    /// <param name="causation"><see cref="Contracts.Sequences.Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Causation"/>.</returns>
    public static Causation ToApi(this Contracts.Sequences.Causation causation) =>
        new(causation.Occurred, causation.Type, causation.Properties);

    /// <summary>
    /// Converts a collection of contract causations to API causations.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Contracts.Sequences.Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Causation"/>.</returns>
    public static IEnumerable<Causation> ToApi(this IEnumerable<Contracts.Sequences.Causation> causations) =>
        causations.Select(c => c.ToApi()).ToArray();

    /// <summary>
    /// Converts an API causation to a storage causation.
    /// </summary>
    /// <param name="causation"><see cref="Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Concepts.Auditing.Causation"/>.</returns>
    public static Concepts.Auditing.Causation ToChronicle(this Causation causation) =>
        new(causation.Occurred, causation.Type, causation.Properties);

    /// <summary>
    /// Converts a collection of API causations to storage causations.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Concepts.Auditing.Causation"/>.</returns>
    public static IEnumerable<Concepts.Auditing.Causation> ToChronicle(this IEnumerable<Causation> causations) =>
        causations.Select(c => c.ToChronicle()).ToArray();
}
