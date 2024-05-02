// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_InstancesOf;

public class when_having_multiple_implementations : Specification
{
    Mock<ITypes> type_finder;
    Mock<IServiceProvider> container;
    IAmAnInterface[] instances;

    OneImplementation one_implementation_instance;
    SecondImplementation second_implementation_instance;

    void Establish()
    {
        type_finder = new();
        type_finder.Setup(t => t.FindMultiple<IAmAnInterface>()).Returns(new Type[]
        {
                typeof(OneImplementation),
                typeof(SecondImplementation)
        });
        container = new();
        one_implementation_instance = new OneImplementation();
        second_implementation_instance = new SecondImplementation();

        container.Setup(c => c.GetService(typeof(OneImplementation))).Returns(one_implementation_instance);
        container.Setup(c => c.GetService(typeof(SecondImplementation))).Returns(second_implementation_instance);
    }

    void Because() => instances = new InstancesOf<IAmAnInterface>(type_finder.Object, container.Object).ToArray();

    [Fact] void should_get_the_implementations() => instances.ShouldContainOnly(one_implementation_instance, second_implementation_instance);
}
