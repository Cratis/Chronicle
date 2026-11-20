// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

[CollectionDefinition(Name, DisableParallelization = true)]
public class AutomaticRemovalCollection : ICollectionFixture<AutomaticRemovalFixture>
{
    public const string Name = "Unseen observer removal, on and swept by the kernel";
}
