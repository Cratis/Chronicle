// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Tagging.for_EventTagResolver.when_resolving;

public class an_event_type_requiring_an_audience : given.an_event_tag_resolver
{
    Exception _withoutAudience;
    IReadOnlyList<NamedTag> _fromCallSite;
    IReadOnlyList<NamedTag> _fromEnricher;
    IReadOnlyList<NamedTag> _fromAmbient;

    void Because()
    {
        _withoutAudience = Catch.Exception(() => Resolve(new RequiredAudienceEvent("x")));

        _existing = [Tag(WellKnownTags.Audience, "from-call-site")];
        _fromCallSite = Resolve(new RequiredAudienceEvent("x"));

        _existing = [];
        _enricher.GetTags(Arg.Any<EventTagContext>()).Returns([Tag(WellKnownTags.Audience, "from-enricher")]);
        _fromEnricher = Resolve(new RequiredAudienceEvent("x"));

        _enricher.GetTags(Arg.Any<EventTagContext>()).Returns([]);
        using (EventTags.Audience("from-scope")) _fromAmbient = Resolve(new RequiredAudienceEvent("x"));
    }

    [Fact] void should_fail_with_missing_audience_when_none_is_provided() => _withoutAudience.ShouldBeOfExactType<MissingAudience>();
    [Fact] void should_accept_an_audience_from_the_call_site() => _fromCallSite.Single().Value.ShouldEqual("from-call-site");
    [Fact] void should_accept_an_audience_from_an_enricher() => _fromEnricher.Single().Value.ShouldEqual("from-enricher");
    [Fact] void should_accept_an_audience_from_the_ambient_scope() => _fromAmbient.Single().Value.ShouldEqual("from-scope");
}
