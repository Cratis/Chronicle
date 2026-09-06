// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_protecting_and_unprotecting_with_the_same_provider : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    string _plaintext;
    string _protectedPayload;
    string _unprotectedPayload;
    IReadOnlyCollection<XElement> _storedKeys;

    void Establish()
    {
        _services = BuildServices();
        _plaintext = "chronicle-protect-within-one-provider";
    }

    void Because()
    {
        var protector = _services.DataProtectionProvider.CreateProtector("chronicle.server.specs.authentication.same-provider");
        _protectedPayload = protector.Protect(_plaintext);
        _unprotectedPayload = protector.Unprotect(_protectedPayload);
        _storedKeys = _services.XmlRepository.GetAllElements();
    }

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_store_the_generated_key_through_the_grain() => _services.DataProtectionKeys.Received(1).StoreKey(Arg.Any<string>(), Arg.Any<string>());
    [Fact] void should_update_the_shared_key_collection() => _services.SharedKeys.Count.ShouldEqual(1);
    [Fact] void should_make_the_current_key_visible_through_the_repository() => Normalize(_storedKeys.Single()).ShouldEqual(Normalize(XElement.Parse(_services.SharedKeys.Snapshot().Single())));
    [Fact] void should_unprotect_with_the_same_provider() => _unprotectedPayload.ShouldEqual(_plaintext);
}
