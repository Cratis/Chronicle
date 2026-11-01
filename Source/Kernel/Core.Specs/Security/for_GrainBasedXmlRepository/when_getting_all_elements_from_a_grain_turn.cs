// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Chronicle.Security.for_GrainBasedXmlRepository;

public class when_getting_all_elements_from_a_grain_turn : given.a_repository_called_from_a_grain_turn
{
    IReadOnlyCollection<XElement> _result;

    async Task Because() => _result = await InGrainTurn(_repository.GetAllElements);

    [Fact] void should_return_the_stored_keys() => _result.Single().ToString().ShouldEqual(XElement.Parse(Key).ToString());
    [Fact] void should_not_need_the_blocked_grain_turn_to_receive_the_reply() => _replyScheduler.ShouldNotEqual(_turn);
}
