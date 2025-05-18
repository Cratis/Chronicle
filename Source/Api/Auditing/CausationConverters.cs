// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Auditing;

/// <summary>
/// Converts between contracts and API models for causation.
/// </summary>
public static class CausationConverters
{
    /// <summary>
    /// Converts a contract causation to an API causation.
    /// </summary>
    /// <param name="causation"><see cref="Contracts.Auditing.Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Causation"/>.</returns>
    public static Causation ToApi(this Contracts.Auditing.Causation causation) => new(
            causation.Occurred,
            causation.Type,
            causation.Properties.ToDictionary(x => x.Key, x => x.Value));

    /// <summary>
    /// Converts a collection of contract causations to API causations.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Contracts.Auditing.Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Causation"/>.</returns>
    public static IEnumerable<Causation> ToApi(this IEnumerable<Contracts.Auditing.Causation> causations) =>
        causations.Select(c => c.ToApi()).ToArray();

    /// <summary>
    /// Converts an API causation to a contract causation.
    /// </summary>
    /// <param name="causation"><see cref="Causation"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Auditing.Causation"/>.</returns>
    public static Contracts.Auditing.Causation ToContract(this Causation causation) => new()
    {
        Occurred = causation.Occurred!,
        Type = causation.Type,
        Properties = causation.Properties.ToDictionary(x => x.Key, x => x.Value)
    };

    /// <summary>
    /// Converts a collection of API causations to contract causations.
    /// </summary>
    /// <param name="causations">Collection of <see cref="Causation"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Contracts.Auditing.Causation"/>.</returns>
    public static IList<Contracts.Auditing.Causation> ToContract(this IEnumerable<Causation> causations) =>
        causations.Select(c => c.ToContract()).ToList();
}
