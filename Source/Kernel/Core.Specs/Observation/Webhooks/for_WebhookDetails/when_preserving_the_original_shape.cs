// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Security;
using ProtoBuf;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDetails;

public class when_preserving_the_original_shape : Specification
{
    WebhookDetails _details;
    object?[] _deconstructed;
    WebhookDetails _extendedDetails;
    object?[] _extendedDeconstructed;
    object[] _extendedValues;
    readonly object[] _originalValues = ["webhook", "identifier", "https://example.com/webhook", "log", Array.Empty<Contracts.Events.EventType>(), AuthorizationType.None, new Dictionary<string, string> { ["header"] = "value" }, true, false];

    void Because()
    {
        var signature = new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(IEnumerable<Contracts.Events.EventType>), typeof(AuthorizationType), typeof(IDictionary<string, string>), typeof(bool), typeof(bool) };
        _details = (WebhookDetails)typeof(WebhookDetails).GetConstructor(signature)!.Invoke(_originalValues);
        var deconstruct = typeof(WebhookDetails).GetMethod(nameof(WebhookDetails.Deconstruct), signature.Select(_ => _.MakeByRefType()).ToArray())!;
        _deconstructed = new object?[9];
        deconstruct.Invoke(_details, _deconstructed);

        Type[] extendedSignature = [.. signature, typeof(EventGenerationDelivery)];
        _extendedValues = [.. _originalValues, EventGenerationDelivery.Pinned];
        _extendedDetails = (WebhookDetails)typeof(WebhookDetails).GetConstructor(extendedSignature)!.Invoke(_extendedValues);
        var extendedDeconstruct = typeof(WebhookDetails).GetMethod(nameof(WebhookDetails.Deconstruct), extendedSignature.Select(_ => _.MakeByRefType()).ToArray())!;
        _extendedDeconstructed = new object?[10];
        extendedDeconstruct.Invoke(_extendedDetails, _extendedDeconstructed);
    }

    [Fact] void should_keep_the_original_binary_constructor() => _details.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_keep_the_original_binary_deconstruction() => _deconstructed.ShouldEqual(_originalValues);
    [Fact] void should_keep_the_generation_policy_on_the_constructor_used_for_generation() => typeof(WebhookDetails).GetConstructors()[0].GetParameters().Length.ShouldEqual(11);
    [Fact] void should_keep_the_extended_deconstruction() => typeof(WebhookDetails).GetMethods().Any(_ => _.Name == nameof(WebhookDetails.Deconstruct) && _.GetParameters().Length == 10).ShouldBeTrue();
    [Fact] void should_keep_the_ten_parameter_binary_constructor() => _extendedDetails.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_default_the_ten_parameter_constructor_to_unsigned() => _extendedDetails.Signing.ShouldEqual(Contracts.Observation.Webhooks.WebhookSigningMode.Disabled);
    [Fact] void should_round_trip_the_ten_field_shape() => _extendedDeconstructed.ShouldEqual(_extendedValues);
    [Fact] void should_include_signing_in_the_eleven_output_deconstruction() => typeof(WebhookDetails).GetMethods().Any(_ => _.Name == nameof(WebhookDetails.Deconstruct) && _.GetParameters().Length == 11).ShouldBeTrue();
    [Fact] void should_keep_signing_at_response_field_eleven() => typeof(Contracts.Observation.Webhooks.WebhookDetailsResponse).GetProperty(nameof(WebhookDetails.Signing))!.GetCustomAttribute<ProtoMemberAttribute>()!.Tag.ShouldEqual(11);
    [Fact] void should_keep_the_generated_response_policy_at_field_ten() => typeof(Contracts.Observation.Webhooks.WebhookDetailsResponse).GetProperty(nameof(WebhookDetails.GenerationDelivery))!.GetCustomAttribute<ProtoMemberAttribute>()!.Tag.ShouldEqual(10);
}
