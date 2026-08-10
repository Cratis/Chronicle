// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_a_key;

public class and_no_store_has_one : given.two_key_stores
{
    EncryptionKey _candidate;
    EncryptionKey _result;
    EncryptionKey? _inPrimary;
    EncryptionKey? _inSecondary;
    int _primaryRevisions;
    int _secondaryRevisions;

    void Establish() => _candidate = KeyNamed("candidate");

    async Task Because()
    {
        _result = await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate);
        _inPrimary = await KeyIn(_primary);
        _inSecondary = await KeyIn(_secondary);
        _primaryRevisions = await RevisionCountIn(_primary);
        _secondaryRevisions = await RevisionCountIn(_secondary);
    }

    [Fact] void should_return_the_candidate_key() => _result.ShouldEqual(_candidate);
    [Fact] void should_provision_it_on_the_primary_store() => _inPrimary.ShouldEqual(_candidate);
    [Fact] void should_mirror_it_to_the_secondary_store() => _inSecondary.ShouldEqual(_candidate);
    [Fact] void should_leave_the_primary_store_with_a_single_revision() => _primaryRevisions.ShouldEqual(1);
    [Fact] void should_leave_the_secondary_store_with_a_single_revision() => _secondaryRevisions.ShouldEqual(1);
}
