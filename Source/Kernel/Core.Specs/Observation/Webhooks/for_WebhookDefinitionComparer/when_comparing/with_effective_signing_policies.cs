// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionComparer.when_comparing;

public class with_effective_signing_policies : Specification
{
    WebhookDefinitionComparer _comparer;
    WebhookDefinition _definition;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Webhooks.Has("webhook").Returns(true);
        _comparer = new(storage, new ObjectComparer(), NullLogger<WebhookDefinitionComparer>.Instance);
        _definition = new Contracts.Observation.Webhooks.WebhookDefinition { Identifier = "webhook" }.ToChronicle();
    }

    [Theory]
    [InlineData(WebhookSigningMode.None, WebhookDefinitionCompareResult.Same)]
    [InlineData(WebhookSigningMode.Ed25519, WebhookDefinitionCompareResult.Different)]
    async Task should_compare_with_the_real_object_comparer(WebhookSigningMode signing, WebhookDefinitionCompareResult expected)
    {
        var result = await _comparer.Compare(new("webhook", "store"), _definition, _definition with { Signing = signing });
        result.Result.ShouldEqual(expected);
    }

    [Fact]
    async Task should_classify_a_combined_target_and_signing_change()
    {
        var updated = _definition with { Signing = WebhookSigningMode.Ed25519, Target = _definition.Target with { Url = "https://example.com/changed" } };
        var result = await _comparer.Compare(new("webhook", "store"), _definition, updated);
        result.ChangedProperties!.SigningChanged.ShouldBeTrue();
        result.ChangedProperties.TargetUrlChanged.ShouldBeTrue();
        result.ChangedProperties.AffectsDeliveredEvents.ShouldBeTrue();
    }
}
