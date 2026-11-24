// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDetails;

public class when_preserving_the_signing_shape : Specification
{
    WebhookDetails _details;
    object?[] _deconstructed;
    readonly object[] _values = ["webhook", "identifier", "https://example.com", "log", Array.Empty<Contracts.Events.EventType>(), AuthorizationType.None, new Dictionary<string, string>(), true, false, EventGenerationDelivery.Pinned, WebhookSigningMode.Ed25519];

    void Because()
    {
        Type[] signature = [typeof(string), typeof(string), typeof(string), typeof(string), typeof(IEnumerable<Contracts.Events.EventType>), typeof(AuthorizationType), typeof(IDictionary<string, string>), typeof(bool), typeof(bool), typeof(EventGenerationDelivery), typeof(WebhookSigningMode)];
        _details = (WebhookDetails)typeof(WebhookDetails).GetConstructor(signature)!.Invoke(_values);
        var deconstruct = typeof(WebhookDetails).GetMethod(nameof(WebhookDetails.Deconstruct), signature.Select(_ => _.MakeByRefType()).ToArray())!;
        _deconstructed = new object?[11];
        deconstruct.Invoke(_details, _deconstructed);
    }

    [Fact] void should_keep_the_eleven_field_constructor_and_deconstruction() => _deconstructed.ShouldEqual(_values);
    [Fact] void should_keep_the_signing_policy() => _details.Signing.ShouldEqual(WebhookSigningMode.Ed25519);
    [Fact] void should_default_legacy_construction_to_no_metadata() => _details.HeaderValuesSet.ShouldBeEmpty();
}
