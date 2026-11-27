// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_another_marker_insert_wins : given.bson_storage
{
    bool _opened;

    void Establish() => _insertFailure = DuplicateKey();

    async Task Because() => _opened = await new NamespaceMarkerStorage(_collection).TryOpen(_identity, null);

    [Fact] void should_report_the_lost_insert() => _opened.ShouldBeFalse();
    [Fact] void should_never_replace_the_winning_marker() => _update.ShouldBeNull();
}
