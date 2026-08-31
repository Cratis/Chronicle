// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.given;

public class a_pattern_miner : Specification
{
    protected static readonly DateTimeOffset Occurred = new(2026, 8, 24, 9, 15, 0, TimeSpan.Zero);

    protected PatternMiner _miner;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;

    void Establish()
    {
        _eventStore = "some-store";
        _namespace = EventStoreNamespaceName.Default;

        var options = Options.Create(new ChronicleOptions
        {
            PatternDetection = new PatternDetection
            {
                Error = 0.001d,
                MinimumSupport = 0.1d,
                MinimumConfidence = 0.5d,
                DecayFactor = 1d
            }
        });

        _miner = new(new FacetVocabulary(options), new FacetSetGenerator(), options);
    }

    protected static EventFeatures Features(
        string groupingKey,
        string commandType,
        DayOfWeek day = DayOfWeek.Monday,
        TimeBucket timeBucket = TimeBucket.Morning,
        DateTimeOffset? occurred = null) =>
        new(
            groupingKey,
            commandType,
            InitiatorType.User,
            groupingKey,
            FacetValue.Unspecified,
            FacetValue.Unspecified,
            FacetValue.Unspecified,
            "ExpenseReport",
            2026,
            8,
            day,
            timeBucket,
            occurred ?? Occurred);
}
