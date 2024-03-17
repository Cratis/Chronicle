// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_ContractToImplementorsMap;

public class when_asking_for_implementors_of_type_without_implementors : given.an_empty_map
{
    IEnumerable<Type> result;

    void Because() => result = map.GetImplementorsFor(typeof(IInterface));

    [Fact] void should_not_have_any_implementors() => result.ShouldBeEmpty();
}
