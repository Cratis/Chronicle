// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;

namespace Cratis.Chronicle.Connections.for_LazyServices;

public class when_getting_occurrences : given.a_lazy_services
{
    IOccurrences _result;

    void Because() => _result = _services.Occurrences;

    [Fact] void should_create_the_service() => ReferenceEquals(_result, _factory.Creations.Single().Service).ShouldBeTrue();
}
