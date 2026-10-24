// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_NativeUpserts.when_building_models;

public class on_sql_server : Specification
{
    IEnumerable<string> _unsupported;

    void Because() => _unsupported = given.model_guard.Unsupported(builder => builder.UseSqlServer("Server=localhost;Database=model-guard;TrustServerCertificate=True"));

    [Fact] void should_use_native_upserts_for_every_mapping() => _unsupported.ShouldBeEmpty();
}
