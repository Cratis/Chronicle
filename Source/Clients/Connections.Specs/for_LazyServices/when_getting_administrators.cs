// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Connections.for_LazyServices;

public class when_getting_administrators : given.a_lazy_services
{
    IAdministrators _result;

    void Because() => _result = _services.Administrators;

    [Fact] void should_create_the_service() => ReferenceEquals(_result, _factory.Creations.Single().Service).ShouldBeTrue();
}
