// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_SqlitePragmaConnectionInterceptor;

public class when_opening_a_read_only_probe : given.a_read_only_probe
{
    void Because() => _error = Catch.Exception(_context.Database.OpenConnection);

    [Fact] void should_allow_the_existence_probe() => _error.ShouldBeNull();
    [Fact] void should_leave_the_journal_mode_unchanged() => JournalMode().ShouldEqual("delete");
    [Fact] void should_reuse_the_existing_namespace_provider() => SharesNamespaceProvider().ShouldBeTrue();
}
