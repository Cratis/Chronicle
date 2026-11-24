// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_upgrading_postgresql_metadata_schema : given.a_schema_upgrade
{
    Task Because() => Exercise(sqlServer: false);

    [Fact] void should_allow_an_established_absent_legacy_marker() => _initiallyClean.ShouldBeTrue();
    [Fact] void should_refuse_malformed_legacy_metadata() => _corruptError.ShouldNotBeNull();
    [Fact] void should_fail_closed_if_legacy_metadata_cannot_be_read() => _error.ShouldNotBeNull();
    [Fact] void should_preserve_the_default_schema_restriction() => _restricted.ShouldBeTrue();
    [Fact] void should_import_the_marker_before_the_old_marker_disappears() => _retained.ShouldBeTrue();
    [Fact] void should_query_the_default_schema() => (_commands.LegacyReads > 0).ShouldBeTrue();
}
