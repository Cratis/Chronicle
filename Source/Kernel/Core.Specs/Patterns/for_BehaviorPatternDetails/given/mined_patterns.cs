// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patterns;
using Microsoft.Extensions.Options;
using KernelBehaviorPattern = Cratis.Chronicle.Concepts.Patterns.BehaviorPattern;

namespace Cratis.Chronicle.Patterns.for_BehaviorPatternDetails.given;

public class mined_patterns : Specification
{
    protected const string EventStore = "some-store";
    protected const string Scope = "user-42";

    protected IStorage _storage;
    protected IFacetVocabulary _vocabulary;
    protected IFacetSetGenerator _generator;
    protected IPatternMatcher _matcher;
    protected IOptions<ChronicleOptions> _options;

    protected IBehaviorPatternStorage _patterns;
    protected KernelBehaviorPattern _mondayMorning;
    protected KernelBehaviorPattern _monday;
    protected KernelBehaviorPattern _lowConfidence;
    protected KernelBehaviorPattern _registersInvoicesOnMondayMornings;
    protected KernelBehaviorPattern _matchesInvoicesOnFridays;

    void Establish()
    {
        _mondayMorning = Pattern([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")], 0.9d);
        _monday = Pattern([new Facet(FacetName.Day, "Monday")], 0.8d);
        _lowConfidence = Pattern([new Facet(FacetName.TimeBucket, "Morning")], 0.2d);

        _registersInvoicesOnMondayMornings = Pattern(
            [
                new Facet(FacetName.CommandType, "RegisterInvoice"),
                new Facet(FacetName.Day, "Monday"),
                new Facet(FacetName.TimeBucket, "Morning")
            ],
            0.95d);

        _matchesInvoicesOnFridays = Pattern(
            [new Facet(FacetName.CommandType, "MatchInvoice"), new Facet(FacetName.Day, "Friday")],
            1d);

        KernelBehaviorPattern[] held =
        [
            _mondayMorning,
            _monday,
            _lowConfidence,
            _registersInvoicesOnMondayMornings,
            _matchesInvoicesOnFridays
        ];

        _patterns = Substitute.For<IBehaviorPatternStorage>();
        _patterns
            .GetMatching(new PatternGroupingKey(Scope), Arg.Any<IEnumerable<FacetSetKey>>())
            .Returns(call => held.Where(pattern => call.Arg<IEnumerable<FacetSetKey>>().Contains(pattern.Facets.Key)));
        _patterns.GetForScope(new PatternGroupingKey(Scope)).Returns(held);

        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _storage.GetEventStore(new EventStoreName(EventStore)).Returns(eventStore);
        eventStore.GetNamespace(EventStoreNamespaceName.Default).Returns(@namespace);
        @namespace.Patterns.Returns(_patterns);

        _options = Options.Create(new ChronicleOptions
        {
            PatternDetection = new PatternDetection { MinimumConfidence = 0.5d }
        });

        _vocabulary = new FacetVocabulary(_options);
        _generator = new FacetSetGenerator();
        _matcher = new PatternMatcher();
    }

    static KernelBehaviorPattern Pattern(IEnumerable<Facet> facets, double confidence) =>
        new(Scope, new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
