// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle;

public static class NamespaceCasingContext
{
    public static INamespaceCasing AddNamespaceCasing(this TestKitSilo silo)
    {
        var casing = Create();
        silo.AddService(casing);
        return casing;
    }

    public static INamespaceCasing Create()
    {
        var casing = Substitute.For<INamespaceCasing>();
        casing.Canonicalize(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(_ => _.Arg<EventStoreNamespaceName>());
        return casing;
    }
}
