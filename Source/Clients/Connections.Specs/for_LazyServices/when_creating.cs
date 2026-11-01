// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LazyServices;

public class when_creating : given.a_lazy_services
{
    void Because() => _services = new LazyServices(_callInvoker, _factory);

    [Fact] void should_not_create_any_service() => _factory.Creations.ShouldBeEmpty();
}
