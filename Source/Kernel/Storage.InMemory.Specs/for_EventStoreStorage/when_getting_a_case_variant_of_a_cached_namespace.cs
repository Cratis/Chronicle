// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorage;

public class when_getting_a_case_variant_of_a_cached_namespace : given.an_event_store_storage_with_name_checks
{
    Exception _error;

    void Establish() => _storage.GetNamespace("tenant");

    void Because() => _error = Catch.Exception(() => _storage.GetNamespace("TENANT"));

    [Fact] void should_refuse_it_as_a_case_variant() => ((ResourceNameConflict)_error).Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_still_serve_the_cached_namespace() => _storage.GetNamespace("tenant").ShouldNotBeNull();
}
