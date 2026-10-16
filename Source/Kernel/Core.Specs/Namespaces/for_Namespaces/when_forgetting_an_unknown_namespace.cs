// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_forgetting_an_unknown_namespace : given.a_namespaces_grain
{
    async Task Because() => await _namespaces.Forget(_namespace);

    [Fact] async Task should_not_record_anything() =>
        await _systemSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
}
