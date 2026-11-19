// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.MongoDB;

namespace Cratis.Chronicle.DevelopmentTools.for_KernelStateResetter;

public class when_mongodb_is_active : given.storage_reset_registrations
{
    ICanPerformKernelStateReset[] _handlers;

    void Because() => _handlers = new[] { true, false }
        .SelectMany(conventionsFirst => ResolveHandlers(StorageType.MongoDB, "mongodb://localhost:27017", conventionsFirst))
        .ToArray();

    [Fact] void should_resolve_one_handler_for_each_registration_order() => _handlers.Length.ShouldEqual(2);
    [Fact] void should_only_construct_the_mongodb_handler() => _handlers.All(_ => _ is MongoDBKernelStateResetHandler).ShouldBeTrue();
    [Fact] void should_be_able_to_reset_the_active_backend() => _handlers.All(_ => _.CanReset()).ShouldBeTrue();
}
