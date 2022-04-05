﻿// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Types.for_ContractToImplementorsMap;

public class when_asking_for_implementors_of_type_by_generic_without_implementors : given.an_empty_map
{
    IEnumerable<Type> result;

    void Because() => result = map.GetImplementorsFor<IInterface>();

    [Fact] void should_not_have_any_implementors() => result.ShouldBeEmpty();
}
