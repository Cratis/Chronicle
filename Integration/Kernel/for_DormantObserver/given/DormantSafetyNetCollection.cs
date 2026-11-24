// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

[CollectionDefinition(Name, DisableParallelization = true)]
public class DormantSafetyNetCollection : ICollectionFixture<DormantSafetyNetFixture>
{
    public const string Name = "Dormant observers with a short safety net";
}
