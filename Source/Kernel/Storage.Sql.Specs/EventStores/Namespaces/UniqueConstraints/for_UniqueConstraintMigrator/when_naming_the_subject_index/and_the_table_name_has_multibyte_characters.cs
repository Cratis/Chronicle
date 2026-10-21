// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_naming_the_subject_index;

/// <summary>
/// A table name that fits PostgreSQL's identifier limit in characters but not in UTF-8 bytes.
/// </summary>
public class and_the_table_name_has_multibyte_characters : Specification
{
    static readonly string _tableName = "claims_" + new string('ø', 25);
    string _subjectIndex;
    string _otherSubjectIndex;

    void Because()
    {
        _subjectIndex = UniqueConstraintMigrator.SubjectIndexName(_tableName);
        _otherSubjectIndex = UniqueConstraintMigrator.SubjectIndexName(_tableName + "ø");
    }

    [Fact] void should_fit_the_postgresql_identifier_limit_in_bytes() => Encoding.UTF8.GetByteCount(_subjectIndex).ShouldBeLessThanOrEqual(63);
    [Fact] void should_be_distinct_per_table() => _subjectIndex.ShouldNotEqual(_otherSubjectIndex);
    [Fact] void should_end_with_the_column_name() => _subjectIndex.EndsWith("_Subject").ShouldBeTrue();
}
