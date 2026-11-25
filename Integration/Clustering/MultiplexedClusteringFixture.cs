// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Cratis.Chronicle.Integration.Clustering.for_MultiplexedObservation;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// A clustering fixture whose clients observe over one multiplexed stream per connection instead of one stream per
/// reactor and reducer, and which records what travels on those streams.
/// </summary>
public class MultiplexedClusteringFixture : ClusteringFixture
{
    readonly List<RecordedMultiplexedObservation> _recordings = [];

    /// <summary>
    /// Gets what each client's multiplexed connection carried, one recording per client.
    /// </summary>
    public IReadOnlyList<RecordedMultiplexedObservation> Recordings
    {
        get
        {
            lock (_recordings)
            {
                return [.. _recordings];
            }
        }
    }

    /// <inheritdoc/>
    protected override Func<IMultiplexedObservation, IMultiplexedObservation>? MultiplexedObservation => kernel =>
    {
        var recording = new RecordedMultiplexedObservation(kernel);
        lock (_recordings)
        {
            _recordings.Add(recording);
        }

        return recording;
    };
}
