// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Setup.for_background_startup;

[CollectionDefinition(Name, DisableParallelization = true)]
public class BackgroundStartupCollection : ICollectionFixture<BackgroundStartupFixture>
{
    public const string Name = "Background startup integration";
}
