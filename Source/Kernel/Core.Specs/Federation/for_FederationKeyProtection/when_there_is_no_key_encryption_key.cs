// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Federation.for_FederationKeyProtection;

public class when_there_is_no_key_encryption_key : Specification
{
    given.KeyEncryptionKeyFile _files;
    Exception _noPath;
    Exception _missingInProduction;
    Exception _wrongLength;
    byte[] _unwrappedInDevelopment;
    string _generatedPath;
    byte[] _wrappedWithTheBackendKey;
    byte[] _unwrappedWithTheBackendKey;
    bool _productionGeneratedAFile;

    void Establish() => _files = new();

    void Destroy() => _files.Dispose();

    async Task Because()
    {
        var key = new byte[] { 1, 2, 3, 4 };

        // Fails closed: no path, and a backend that keeps its internal secrets in the database it protects.
        _noPath = await Catch.Exception(() => given.KeyEncryptionKeyFile.Protection(null).Protect(key, "identity", "id", "1"));

        // Fails closed: production never generates the file.
        var missing = _files.Missing;
        _missingInProduction = await Catch.Exception(() => given.KeyEncryptionKeyFile.Protection(missing).Protect(key, "identity", "id", "1"));
        _productionGeneratedAFile = File.Exists(missing);

        // A file that is not 32 bytes of base64 is not a key.
        var wrong = _files.Missing;
        await File.WriteAllTextAsync(wrong, "c2hvcnQ=");
        _wrongLength = await Catch.Exception(() => given.KeyEncryptionKeyFile.Protection(wrong).Protect(key, "identity", "id", "1"));

        // Development generates the file when it is missing.
        _generatedPath = _files.Missing;
        var development = given.KeyEncryptionKeyFile.Protection(_generatedPath, development: true);
        var wrapped = await development.Protect(key, "identity", "id", "1");
        _unwrappedInDevelopment = await given.KeyEncryptionKeyFile.Protection(_generatedPath).Unprotect(wrapped, "identity", "id", "1");

        // A backend that holds internal secrets outside the databases may supply the key itself.
        var backendKey = new EncryptionKey(new byte[16], System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var custody = Substitute.For<IEncryptionKeyStorage>();
        custody.KeepsInternalSecretsOutsideTheEventStoreDatabases.Returns(true);
        custody.GetOrAddInternalSecretFor(EventStoreName.System, InternalSecretPurpose.FederationKeyProtection, Arg.Any<EncryptionKey>()).Returns(backendKey);
        var viaBackend = given.KeyEncryptionKeyFile.Protection(null, custody: custody);
        _wrappedWithTheBackendKey = await viaBackend.Protect(key, "identity", "id", "1");
        _unwrappedWithTheBackendKey = await viaBackend.Unprotect(_wrappedWithTheBackendKey, "identity", "id", "1");
    }

    [Fact] void should_refuse_without_a_path_when_the_backend_keeps_secrets_in_the_database() => _noPath.ShouldBeOfExactType<FederationKeyEncryptionKeyUnavailable>();
    [Fact] void should_refuse_in_production_when_the_file_is_missing() => _missingInProduction.ShouldBeOfExactType<FederationKeyEncryptionKeyUnavailable>();
    [Fact] void should_not_generate_a_file_in_production() => _productionGeneratedAFile.ShouldBeFalse();
    [Fact] void should_refuse_a_file_that_is_not_a_key() => _wrongLength.ShouldBeOfExactType<FederationKeyEncryptionKeyUnavailable>();
    [Fact] void should_generate_the_file_in_development() => File.Exists(_generatedPath).ShouldBeTrue();
    [Fact] void should_work_with_the_generated_file() => _unwrappedInDevelopment.ShouldEqual(new byte[] { 1, 2, 3, 4 });
    [Fact] void should_work_with_a_backend_that_keeps_internal_secrets_outside_the_databases() => _unwrappedWithTheBackendKey.ShouldEqual(new byte[] { 1, 2, 3, 4 });
    [Fact] void should_not_leak_the_path_or_a_key_in_the_reason() => _wrongLength.Message.ShouldNotContain("c2hvcnQ=");
}
