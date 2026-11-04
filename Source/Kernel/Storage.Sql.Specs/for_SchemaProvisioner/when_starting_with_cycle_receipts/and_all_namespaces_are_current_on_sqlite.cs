// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts;

public class and_all_namespaces_are_current_on_sqlite : given.provisioned_namespaces
{
    async Task Because() => _error = await Catch.Exception(() => Start());

    [Fact] void should_start() => _error.ShouldBeNull();
}
