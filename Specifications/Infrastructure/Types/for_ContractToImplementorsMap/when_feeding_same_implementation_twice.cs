// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_ContractToImplementorsMap;

public class when_feeding_same_implementation_twice : given.an_empty_map
{
    void Establish() => map.Feed(new[] { typeof(ImplementationOfInterface) });

    void Because() => map.Feed(new[] { typeof(ImplementationOfInterface) });

    [Fact] void should_only_return_one_implementor() => map.GetImplementorsFor(typeof(IInterface)).Count().ShouldEqual(1);
}
