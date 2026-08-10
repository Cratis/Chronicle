// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Setup.for_ComplianceEncryptionKeyStorageExtensions.when_registering_a_dedicated_store;

public class and_there_is_no_default_storage : given.a_chronicle_builder
{
    IEncryptionKeyStorage _resolved;
    Exception _error;

    void Because()
    {
        _error = Catch.Exception(() => RegisterTheDedicatedStorage(migrate: true));
        _resolved = Resolve();
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_resolve_the_dedicated_storage_on_its_own() => _resolved.ShouldEqual(_dedicatedStorage);
}
