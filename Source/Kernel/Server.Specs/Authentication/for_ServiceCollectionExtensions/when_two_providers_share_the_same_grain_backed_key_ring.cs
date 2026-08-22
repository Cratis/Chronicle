// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_two_providers_share_the_same_grain_backed_key_ring : given.chronicle_authentication_services
{
    SharedDataProtectionKeys _sharedKeys;
    ChronicleAuthenticationServices _first;
    ChronicleAuthenticationServices _second;
    string _plaintext;
    string _protectedPayload;
    string _unprotectedPayload;

    void Establish()
    {
        _sharedKeys = new();
        _first = BuildServices(_sharedKeys);
        _second = BuildServices(_sharedKeys);
        _plaintext = "chronicle-cross-provider-roundtrip";
    }

    void Because()
    {
        _protectedPayload = _first.DataProtectionProvider
            .CreateProtector("chronicle.server.specs.authentication.cross-provider")
            .Protect(_plaintext);
        _unprotectedPayload = _second.DataProtectionProvider
            .CreateProtector("chronicle.server.specs.authentication.cross-provider")
            .Unprotect(_protectedPayload);
    }

    void Destroy()
    {
        _first.ServiceProvider.Dispose();
        _second.ServiceProvider.Dispose();
    }

    [Fact] void should_store_the_key_once_in_shared_grain_backed_storage() => _sharedKeys.Count.ShouldEqual(1);
    [Fact] void should_allow_the_second_provider_to_unprotect_without_any_filesystem_sharing() => _unprotectedPayload.ShouldEqual(_plaintext);
    [Fact] void should_make_the_shared_key_visible_to_the_second_provider() => _second.XmlRepository.GetAllElements().Count.ShouldEqual(_sharedKeys.Count);
}
