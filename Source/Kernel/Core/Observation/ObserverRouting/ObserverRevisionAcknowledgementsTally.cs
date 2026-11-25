// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Turns the answers of the silos to the publication of a revision into <see cref="ObserverRevisionAcknowledgements"/>.
/// </summary>
public static class ObserverRevisionAcknowledgementsTally
{
    /// <summary>
    /// Tallies the answers of the silos.
    /// </summary>
    /// <param name="revision">The <see cref="ObserverDefinitionRevision"/> that was published; none for a deleted definition.</param>
    /// <param name="answers">The answer of each silo: the revision it holds, or null when it did not answer.</param>
    /// <returns>The <see cref="ObserverRevisionAcknowledgements"/>.</returns>
    public static ObserverRevisionAcknowledgements Tally(
        ObserverDefinitionRevision revision,
        IEnumerable<(SiloAddress Silo, ObserverDefinitionRevision? Held)> answers)
    {
        var acknowledged = 0;
        var lagging = new List<SiloAddress>();
        foreach (var (silo, held) in answers)
        {
            if (held is not null && IsAcknowledged(revision, held))
            {
                acknowledged++;
            }
            else
            {
                lagging.Add(silo);
            }
        }

        return new(acknowledged, lagging);
    }

    static bool IsAcknowledged(ObserverDefinitionRevision revision, ObserverDefinitionRevision held) =>
        revision == ObserverDefinitionRevision.None ? held == ObserverDefinitionRevision.None : held.Value >= revision.Value;
}
