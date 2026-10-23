// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_naming_the_subject_index;

public class and_the_table_name_is_short : Specification
{
    string _result;

    void Because() => _result = UniqueConstraintMigrator.SubjectIndexName("claims");

    [Fact] void should_keep_the_conventional_name() => _result.ShouldEqual("IX_claims_Subject");
}
