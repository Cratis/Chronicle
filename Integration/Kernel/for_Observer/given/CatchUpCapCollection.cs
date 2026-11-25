// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// Collection for the catch-up cap specifications; they run against a kernel that allows two catch-up jobs at once.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class CatchUpCapCollection : ICollectionFixture<CatchUpCapFixture>
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Observer catch-up cap";
}
