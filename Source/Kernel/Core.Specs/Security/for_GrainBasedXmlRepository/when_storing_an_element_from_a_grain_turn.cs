// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Chronicle.Security.for_GrainBasedXmlRepository;

public class when_storing_an_element_from_a_grain_turn : given.a_repository_called_from_a_grain_turn
{
    async Task Because() => await InGrainTurn(() =>
    {
        _repository.StoreElement(XElement.Parse(Key), "key-1");
        return true;
    });

    [Fact] void should_store_the_key() => _keys.Received(1).StoreKey("key-1", XElement.Parse(Key).ToString());
    [Fact] void should_not_need_the_blocked_grain_turn_to_receive_the_reply() => _replyScheduler.ShouldNotEqual(_turn);
}
