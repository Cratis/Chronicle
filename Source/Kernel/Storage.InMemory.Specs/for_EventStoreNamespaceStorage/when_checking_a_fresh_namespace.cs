// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage;

public class when_checking_a_fresh_namespace : given.a_namespace_storage
{
    bool _hasData;

    async Task Because() => _hasData = await _storage.HasData();

    [Fact] void should_report_no_data() => _hasData.ShouldBeFalse();
}
