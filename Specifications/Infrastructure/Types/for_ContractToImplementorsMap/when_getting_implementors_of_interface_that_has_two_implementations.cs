// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_ContractToImplementorsMap;

public class when_getting_implementors_of_interface_that_has_two_implementations : given.an_empty_map
{
    IEnumerable<Type> result;

    void Establish() => map.Feed(new[] { typeof(ImplementationOfInterface), typeof(SecondImplementationOfInterface) });

    void Because() => result = map.GetImplementorsFor(typeof(IInterface));

    [Fact] void should_have_both_the_implementations_only() => result.ShouldContainOnly(typeof(ImplementationOfInterface), typeof(SecondImplementationOfInterface));
}
