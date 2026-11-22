// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

public class when_checking_an_absent_namespace : given.a_namespace_database
{
    IReadOnlyList<string> _before;
    bool _hasData;

    void Establish() => _before = FilesOnDisk;

    async Task Because() => _hasData = await HasData();

    [Fact] void should_report_no_data() => _hasData.ShouldBeFalse();
    [Fact] void should_not_create_a_namespace_database() => FilesOnDisk.ShouldEqual(_before);
}
