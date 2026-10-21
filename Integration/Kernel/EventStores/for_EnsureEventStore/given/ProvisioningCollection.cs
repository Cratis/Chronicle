// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.EventStores.for_EnsureEventStore.given;

[CollectionDefinition(Name)]
public class ProvisioningCollection : ICollectionFixture<ProvisioningFixture>
{
    public const string Name = "Event store provisioning";
}
