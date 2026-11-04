// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Isolates the silo with the test-only outgoing subscriber lifecycle filter.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class SubscriberLifecycleCollection : ICollectionFixture<SubscriberLifecycleFixture>
{
    /// <summary>
    /// The collection's name.
    /// </summary>
    public const string Name = "Definition change during subscriber lifecycle";
}
