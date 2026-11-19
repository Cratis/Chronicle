// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sql;

namespace Cratis.Chronicle.DevelopmentTools.for_KernelStateResetter;

public class when_sql_is_active : given.storage_reset_registrations
{
    ICanPerformKernelStateReset[] _handlers;

    void Because() => _handlers = new[]
        {
            (StorageType.Sqlite, "Data Source=:memory:"),
            (StorageType.PostgreSql, "Host=localhost;Database=chronicle"),
            (StorageType.MsSql, "Server=localhost;Database=chronicle")
        }
        .SelectMany(storage => new[] { true, false }
            .SelectMany(conventionsFirst => ResolveHandlers(storage.Item1, storage.Item2, conventionsFirst)))
        .ToArray();

    [Fact] void should_resolve_one_handler_for_each_provider_and_registration_order() => _handlers.Length.ShouldEqual(6);
    [Fact] void should_only_construct_the_sql_handler() => _handlers.All(_ => _ is SqlKernelStateResetHandler).ShouldBeTrue();
    [Fact] void should_be_able_to_reset_the_active_backend() => _handlers.All(_ => _.CanReset()).ShouldBeTrue();
}
