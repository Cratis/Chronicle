// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_mapping_a_lock_timeout : Specification
{
    int _code;

    void Because() => _code = SchemaExitCode.For(new DatabaseSchemaLockNotAcquired("cluster", "schema", "timeout"), false);

    [Fact] void should_return_three() => _code.ShouldEqual(3);
}
