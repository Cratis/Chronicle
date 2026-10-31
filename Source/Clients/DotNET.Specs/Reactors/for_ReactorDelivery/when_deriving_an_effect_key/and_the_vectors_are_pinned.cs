// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_deriving_an_effect_key;

/// <summary>
/// Keys are stored with events for the life of the event, so the derivation must never drift.
/// </summary>
public class and_the_vectors_are_pinned : Specification
{
    ReactorDelivery _delivery;

    void Establish() => _delivery = new("reactor-1", "orders", "default", EventSequenceId.Log, "order-42", 7UL);

    [Fact] void should_pin_the_first_returned_effect() => _delivery.ReturnedEffectKey(0).Value.ShouldEqual("DFD0A74B71E751142CCDAB0FF4128C97988B0BB106ADCDAE607947F171436DF3");
    [Fact] void should_pin_the_second_returned_effect() => _delivery.ReturnedEffectKey(1).Value.ShouldEqual("4D17FEA704EDEFEFDA03C350E09E858BCB45D8277002060B8C5F6B826F5187E6");
    [Fact] void should_pin_the_epoch() => (_delivery with { EffectEpoch = 1 }).ReturnedEffectKey(0).Value.ShouldEqual("99AE1EE98E2A7D566C2725C4E43EBF3E87AFA98B9FDE741C8F97E8C8EE3B68EC");
    [Fact] void should_pin_a_named_effect() => _delivery.IdempotencyKeyFor("charge").Value.ShouldEqual("A7CE1AEB99AC86BC01CFF1A27C1FCCEDEF2264C86E08C38A38FABD77FFEDD5B4");
}
