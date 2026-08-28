// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patterns;
using Microsoft.Extensions.Options;
using KernelBehaviorPattern = Cratis.Chronicle.Concepts.Patterns.BehaviorPattern;

namespace Cratis.Chronicle.Services.Patterns.for_Patterns.given;

public class a_patterns_service : Specification
{
    protected const string EventStore = "some-store";
    protected const string Scope = "user-42";

    private protected Chronicle.Services.Patterns.Patterns _service;

    protected IBehaviorPatternStorage _patterns;
    protected KernelBehaviorPattern _mondayMorning;
    protected KernelBehaviorPattern _monday;
    protected KernelBehaviorPattern _lowConfidence;

    void Establish()
    {
        _mondayMorning = Pattern([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")], 0.9d);
        _monday = Pattern([new Facet(FacetName.Day, "Monday")], 0.8d);
        _lowConfidence = Pattern([new Facet(FacetName.TimeBucket, "Morning")], 0.2d);

        KernelBehaviorPattern[] held = [_mondayMorning, _monday, _lowConfidence];

        _patterns = Substitute.For<IBehaviorPatternStorage>();
        _patterns
            .GetMatching(new PatternGroupingKey(Scope), Arg.Any<IEnumerable<FacetSetKey>>())
            .Returns(call => held.Where(pattern => call.Arg<IEnumerable<FacetSetKey>>().Contains(pattern.Facets.Key)));
        _patterns.GetForScope(new PatternGroupingKey(Scope)).Returns(held);

        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(new EventStoreName(EventStore)).Returns(eventStore);
        eventStore.GetNamespace(EventStoreNamespaceName.Default).Returns(@namespace);
        @namespace.Patterns.Returns(_patterns);

        var options = Options.Create(new ChronicleOptions
        {
            PatternDetection = new PatternDetection { MinimumConfidence = 0.5d }
        });

        _service = new(storage, new FacetVocabulary(options), new FacetSetGenerator(), new PatternMatcher(), options);
    }

    static KernelBehaviorPattern Pattern(IEnumerable<Facet> facets, double confidence) =>
        new(Scope, new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
