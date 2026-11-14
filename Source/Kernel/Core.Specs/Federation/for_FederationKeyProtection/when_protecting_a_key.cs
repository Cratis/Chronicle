// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Federation.for_FederationKeyProtection;

public class when_protecting_a_key : Specification
{
    const string Identity = "spiffe://example/customer/acme/kernel/prod";
    const string Serial = "0A1B2C";

    given.KeyEncryptionKeyFile _files;
    string _path;
    FederationKeyProtection _protection;
    byte[] _key;
    byte[] _wrapped;
    byte[] _unwrapped;
    Exception _wrongSerial;
    Exception _wrongIdentity;
    Exception _wrongPurpose;
    Exception _anotherKek;
    byte[] _unwrappedAfterRotation;
    Exception _tampered;

    void Establish()
    {
        _files = new();
        _path = _files.Create();
        _protection = given.KeyEncryptionKeyFile.Protection(_path);
        _key = RandomNumberGenerator.GetBytes(48);
    }

    void Destroy() => _files.Dispose();

    async Task Because()
    {
        _wrapped = await _protection.Protect(_key, "identity", Identity, Serial);
        _unwrapped = await _protection.Unprotect(_wrapped, "identity", Identity, Serial);
        _wrongSerial = await Catch.Exception(() => _protection.Unprotect(_wrapped, "identity", Identity, "FFFF"));
        _wrongIdentity = await Catch.Exception(() => _protection.Unprotect(_wrapped, "identity", "spiffe://example/customer/other/kernel/prod", Serial));
        _wrongPurpose = await Catch.Exception(() => _protection.Unprotect(_wrapped, "authority", Identity, Serial));

        var another = given.KeyEncryptionKeyFile.Protection(_files.Create());
        _anotherKek = await Catch.Exception(() => another.Unprotect(_wrapped, "identity", Identity, Serial));

        // Rotation: the new key-encryption key is current and the old one is kept as the previous one, so what was wrapped under it still opens.
        var rotated = given.KeyEncryptionKeyFile.Protection(_files.Create(), previousPath: _path);
        _unwrappedAfterRotation = await rotated.Unprotect(_wrapped, "identity", Identity, Serial);

        var tampered = (byte[])_wrapped.Clone();
        tampered[^1] ^= 1;
        _tampered = await Catch.Exception(() => _protection.Unprotect(tampered, "identity", Identity, Serial));
    }

    [Fact] void should_unwrap_to_the_same_key() => _unwrapped.ShouldEqual(_key);
    [Fact] void should_not_contain_the_key() => _wrapped.AsSpan().IndexOf(_key.AsSpan(0, 16)).ShouldEqual(-1);
    [Fact] void should_not_unwrap_for_another_serial() => _wrongSerial.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_not_unwrap_for_another_identity() => _wrongIdentity.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_not_unwrap_for_another_purpose() => _wrongPurpose.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_not_unwrap_under_another_key_encryption_key() => _anotherKek.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_not_unwrap_when_it_was_tampered_with() => _tampered.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_unwrap_under_the_previous_key_encryption_key_after_a_rotation() => _unwrappedAfterRotation.ShouldEqual(_key);
}
