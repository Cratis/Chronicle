// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_OldKernelOccurrences;

[CollectionDefinition(Name, DisableParallelization = true)]
public class OldKernelCollection : ICollectionFixture<OldKernelFixture>
{
    public const string Name = "Old kernel occurrence compatibility";
}
