// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_TypeUniverse;

public class when_getting_the_universe_for_a_container_without_type_discovery : Specification
{
    ServiceProvider _serviceProvider;
    ITypes _result;
    ITypes _current;

    void Establish() => _serviceProvider = new ServiceCollection().BuildServiceProvider();

    void Because()
    {
        _result = TypeUniverse.For(_serviceProvider);
        _current = TypesServiceCollectionExtensions.CurrentTypeUniverse();
    }

    void Destroy() => _serviceProvider.Dispose();

    [Fact] void should_return_the_current_type_universe() => ReferenceEquals(_result, _current).ShouldBeTrue();
}
