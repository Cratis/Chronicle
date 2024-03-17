// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_ContractToImplementorsMap;

public class when_feeding_two_times : given.an_empty_map
{
    void Establish() => map.Feed(new[] { typeof(ImplementationOfInterface) });

    void Because() => map.Feed(new[] { typeof(SecondImplementationOfInterface) });

    [Fact] void should_have_both_the_implementations_only() => map.GetImplementorsFor(typeof(IInterface)).ShouldContainOnly(typeof(ImplementationOfInterface), typeof(SecondImplementationOfInterface));
}
