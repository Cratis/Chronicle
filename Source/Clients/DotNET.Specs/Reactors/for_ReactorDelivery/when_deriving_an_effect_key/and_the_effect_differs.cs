// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_deriving_an_effect_key;

public class and_the_effect_differs : given.a_delivery
{
    IdempotencyKey _returnedZero;
    IdempotencyKey _returnedOne;
    IdempotencyKey _namedZero;
    IdempotencyKey _namedOther;
    IdempotencyKey _returnedZeroInNewEpoch;
    ReactorDelivery _newEpoch;

    void Because()
    {
        _newEpoch = _delivery with { EffectEpoch = 1 };
        _returnedZero = _delivery.ReturnedEffectKey(0);
        _returnedOne = _delivery.ReturnedEffectKey(1);
        _namedZero = _delivery.IdempotencyKeyFor("0");
        _namedOther = _delivery.IdempotencyKeyFor("1");
        _returnedZeroInNewEpoch = _newEpoch.ReturnedEffectKey(0);
    }

    [Fact] void should_differ_by_index() => _returnedZero.ShouldNotEqual(_returnedOne);
    [Fact] void should_differ_by_kind_for_the_same_text() => _returnedZero.ShouldNotEqual(_namedZero);
    [Fact] void should_differ_by_effect_name() => _namedZero.ShouldNotEqual(_namedOther);
    [Fact] void should_differ_by_epoch() => _returnedZero.ShouldNotEqual(_returnedZeroInNewEpoch);
    [Fact] void should_not_change_the_delivery_identity_with_the_epoch() => _newEpoch.Id.ShouldEqual(_delivery.Id);
}
