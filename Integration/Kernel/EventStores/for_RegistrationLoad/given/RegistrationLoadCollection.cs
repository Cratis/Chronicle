// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.EventStores.for_RegistrationLoad.given;

[CollectionDefinition(Name)]
public class RegistrationLoadCollection : ICollectionFixture<RegistrationLoadFixture>
{
    public const string Name = "Registration load";
}
