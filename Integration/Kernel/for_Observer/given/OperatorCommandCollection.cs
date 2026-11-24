// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// Collection for the operator command specifications; they share one kernel and run one at a time.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class OperatorCommandCollection : ICollectionFixture<OperatorCommandFixture>
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Observer operator commands";
}
