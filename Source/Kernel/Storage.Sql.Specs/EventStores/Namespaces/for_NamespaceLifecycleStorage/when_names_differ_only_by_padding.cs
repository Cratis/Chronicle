// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceLifecycleStorage;

public class when_names_differ_only_by_padding : given.a_lifecycle_storage
{
    NamespaceLifecycle _padded;
    NamespaceLifecycle? _original;
    NamespaceLifecycle? _result;
    NamespaceLifecycle? _missing;
    bool _written;

    async Task Establish()
    {
        await _storage.TryWrite(_name, null, Open());
        _padded = NamespaceLifecycle.Open("Orders ", NamespaceIncarnationId.New(), _now);
    }

    async Task Because()
    {
        _written = await _storage.TryWrite("Orders ", null, _padded);
        _original = await CreateStorage().Get(_name);
        _result = await CreateStorage().Get("ORDERS ");
        _missing = await CreateStorage().Get("Missing");
    }

    [Fact] void should_not_depend_on_sql_padding_comparisons() => _written.ShouldBeTrue();
    [Fact] void should_keep_the_unpadded_identity() => _original.ShouldEqual(Open());
    [Fact] void should_read_the_padded_identity() => _result.ShouldEqual(_padded);
    [Fact] void should_return_null_for_an_absent_name() => _missing.ShouldBeNull();
}
