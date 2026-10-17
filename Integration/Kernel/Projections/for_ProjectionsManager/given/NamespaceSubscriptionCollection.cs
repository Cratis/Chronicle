// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.given;

[CollectionDefinition(Name)]
public class NamespaceSubscriptionCollection : ICollectionFixture<NamespaceSubscriptionFixture>
{
    public const string Name = "Namespace projection subscriptions";
}
