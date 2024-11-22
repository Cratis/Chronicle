// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
namespace Cratis.Chronicle.Grains.Observation.for_FailedPartitionGrainStorageProvider.given;

public class the_provider : Specification
{
    protected FailedPartitionGrainStorageProvider provider;
    protected IStorage storage;

    void Establish()
    {
        storage = Substitute.For<IStorage>();
        provider = new(storage);
    }
}
