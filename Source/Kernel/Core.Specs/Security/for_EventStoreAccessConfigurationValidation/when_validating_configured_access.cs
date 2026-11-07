// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_EventStoreAccessConfigurationValidation;

public class when_validating_configured_access : Specification
{
    [Theory]
    [InlineData("issuer", "subject", "orders", false, true)]
    [InlineData("issuer", "subject", null, true, true)]
    [InlineData("issuer", "", "orders", false, false)]
    [InlineData("", "subject", "orders", false, false)]
    [InlineData("issuer", "subject", "System", false, false)]
    [InlineData("issuer", "subject", "sYsTeM", false, false)]
    [InlineData("issuer", "subject", null, false, false)]
    [InlineData("issuer", "subject", "orders", true, false)]
    void should_require_an_explicit_principal_and_store_scope(string issuer, string subject, string? store, bool all, bool valid)
    {
        var options = new ChronicleOptions
        {
            Authentication = new()
            {
                EventStoreAccess = [new() { Issuer = issuer, Subject = subject, EventStore = store, AllEventStores = all, Access = StoreAccess.Read }]
            }
        };
        new EventStoreAccessConfigurationValidation().Validate(null, options).Succeeded.ShouldEqual(valid);
    }

    [Theory]
    [InlineData("System", false, StoreAccess.Read, false)]
    [InlineData(null, false, StoreAccess.Read, false)]
    [InlineData(null, true, StoreAccess.None, false)]
    [InlineData(null, true, (StoreAccess)8, false)]
    [InlineData("orders", false, StoreAccess.Read, true)]
    void should_validate_bootstrap_client_grants(string? store, bool all, StoreAccess access, bool valid)
    {
        var options = new ChronicleOptions
        {
            Clients = [new() { ClientId = "client", EventStores = [new() { EventStore = store, AllEventStores = all, Access = access }] }]
        };
        new EventStoreAccessConfigurationValidation().Validate(null, options).Succeeded.ShouldEqual(valid);
    }
}
