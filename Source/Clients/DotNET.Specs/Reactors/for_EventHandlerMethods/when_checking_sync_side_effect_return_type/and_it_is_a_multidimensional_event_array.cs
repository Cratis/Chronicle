// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_EventHandlerMethods.when_checking_sync_side_effect_return_type;

public class and_it_is_a_multidimensional_event_array : Specification
{
    bool _result;

    void Because() => _result = EventHandlerMethods.IsValidSyncSideEffectReturnType(
        typeof(TheEvent).MakeArrayType(2),
        [typeof(TheEvent)]);

    [Fact] void should_not_be_recognized_as_a_side_effect() => _result.ShouldBeFalse();

    record TheEvent;
}
