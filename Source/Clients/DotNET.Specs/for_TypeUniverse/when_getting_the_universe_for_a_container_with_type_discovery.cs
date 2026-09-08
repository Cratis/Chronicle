// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_TypeUniverse;

public class when_getting_the_universe_for_a_container_with_type_discovery : Specification
{
    ITypes _types;
    ServiceProvider _serviceProvider;
    ITypes _result;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _serviceProvider = new ServiceCollection().AddSingleton(_types).BuildServiceProvider();
    }

    void Because() => _result = TypeUniverse.For(_serviceProvider);

    void Destroy() => _serviceProvider.Dispose();

    [Fact] void should_return_the_universe_from_the_container() => ReferenceEquals(_result, _types).ShouldBeTrue();
}
