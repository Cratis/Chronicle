// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

[CollectionDefinition(Name, DisableParallelization = true)]
public class DisabledRemovalCollection : ICollectionFixture<DisabledRemovalFixture>
{
    public const string Name = "Unseen observer removal, off by default";
}
