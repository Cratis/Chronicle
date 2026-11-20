// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionComparer.when_comparing;

public class with_only_signing_changed : Specification
{
    WebhookDefinition _definition;
    WebhookDefinitionComparer _comparer;
    WebhookDefinitionComparisonResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Webhooks.Has("webhook").Returns(true);
        var objectComparer = Substitute.For<IObjectComparer>();
        objectComparer.Compare(Arg.Any<object>(), Arg.Any<object>(), out Arg.Any<IEnumerable<PropertyDifference>>()).ReturnsForAnyArgs(true);
        _comparer = new(storage, objectComparer, NullLogger<WebhookDefinitionComparer>.Instance);
        _definition = new Contracts.Observation.Webhooks.WebhookDefinition { Identifier = "webhook" }.ToChronicle();
    }

    async Task Because() => _result = await _comparer.Compare(new("webhook", "store"), _definition, _definition with { Signing = WebhookSigningMode.Ed25519 });

    [Fact] void should_detect_the_change() => _result.Result.ShouldEqual(WebhookDefinitionCompareResult.Different);
    [Fact] void should_identify_signing() => _result.ChangedProperties!.SigningChanged.ShouldBeTrue();
    [Fact] void should_not_affect_delivered_events() => _result.ChangedProperties!.AffectsDeliveredEvents.ShouldBeFalse();
}
