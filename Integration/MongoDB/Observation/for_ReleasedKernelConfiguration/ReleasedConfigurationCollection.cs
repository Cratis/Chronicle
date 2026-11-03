// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelConfiguration;

/// <summary>
/// Process environment mutation must not overlap any other collection, and needs no infrastructure fixture.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class ReleasedConfigurationCollection
{
    public const string Name = "Released configuration binding";
}
