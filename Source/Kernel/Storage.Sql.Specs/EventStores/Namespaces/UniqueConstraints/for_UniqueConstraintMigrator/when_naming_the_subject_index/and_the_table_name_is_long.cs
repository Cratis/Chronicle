// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_naming_the_subject_index;

public class and_the_table_name_is_long : Specification
{
    const string TableName = "claims_0a94400bb0b5436881936fb4a9ceb8fc_with_long_value_index";
    string _subjectIndex;
    string _otherSubjectIndex;
    string _valueIndexAsPostgreSqlSeesIt;

    void Because()
    {
        _subjectIndex = UniqueConstraintMigrator.SubjectIndexName(TableName);
        _otherSubjectIndex = UniqueConstraintMigrator.SubjectIndexName(TableName + "_values");
        _valueIndexAsPostgreSqlSeesIt = ("IX_" + TableName + "_Value")[..63];
    }

    [Fact] void should_fit_the_postgresql_identifier_limit() => _subjectIndex.Length.ShouldBeLessThanOrEqual(63);
    [Fact] void should_not_collide_with_the_value_index() => _subjectIndex.ShouldNotEqual(_valueIndexAsPostgreSqlSeesIt);
    [Fact] void should_be_distinct_per_table() => _subjectIndex.ShouldNotEqual(_otherSubjectIndex);
    [Fact] void should_end_with_the_column_name() => _subjectIndex.EndsWith("_Subject").ShouldBeTrue();
}
