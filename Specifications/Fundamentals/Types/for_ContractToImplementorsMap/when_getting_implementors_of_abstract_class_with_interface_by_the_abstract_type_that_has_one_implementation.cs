// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Types.for_ContractToImplementorsMap;

public class when_getting_implementors_of_abstract_class_with_interface_by_the_abstract_type_that_has_one_implementation : given.an_empty_map
{
    IEnumerable<Type> result;

    void Establish() => map.Feed(new[] { typeof(ImplementationOfAbstractClassWithInterface) });

    void Because() => result = map.GetImplementorsFor(typeof(AbstractClass));

    [Fact] void should_have_the_implementation_only() => result.ShouldContainOnly(typeof(ImplementationOfAbstractClassWithInterface));
}
