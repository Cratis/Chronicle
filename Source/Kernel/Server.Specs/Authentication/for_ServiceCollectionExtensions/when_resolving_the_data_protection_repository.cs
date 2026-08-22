// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_resolving_the_data_protection_repository : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;

    void Establish() => _services = BuildServices();

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_register_the_interface_as_the_concrete_repository() => ReferenceEquals(_services.ConcreteRepository, _services.XmlRepository).ShouldBeTrue();
    [Fact] void should_configure_key_management_to_use_that_same_repository() => ReferenceEquals(_services.ConcreteRepository, _services.KeyManagementOptions.Value.XmlRepository).ShouldBeTrue();
}
