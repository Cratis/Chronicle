// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_provisioning_identifiers_that_differ_only_by_case_or_trailing_space;

public abstract class conformance(given.KeyStoreHarness harness) : given.a_key_store(harness)
{
    ComplianceKey[] _provisioned;
    ComplianceKey[] _readBack;

    async Task Because()
    {
        var provisioned = new List<ComplianceKey>();
        for (var index = 0; index < _identifiers.Length; index++)
        {
            provisioned.Add(await Provision(_identifiers[index], index));
        }
        _provisioned = [.. provisioned];

        var readBack = new List<ComplianceKey>();
        foreach (var identifier in _identifiers)
        {
            readBack.Add(await Read(identifier));
        }
        _readBack = [.. readBack];
    }

    [Fact] public void should_give_each_identifier_its_own_key() => _provisioned.Select(_ => Convert.ToHexString(_.Public)).Distinct().Count().ShouldEqual(_identifiers.Length);

    [Fact]
    public void should_read_each_identifier_back_with_its_own_key()
    {
        for (var index = 0; index < _identifiers.Length; index++)
        {
            _readBack[index].Public.ShouldEqual(KeyFor(index).Public);
        }
    }
}
