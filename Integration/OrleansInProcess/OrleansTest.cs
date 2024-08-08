// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.OrleansInProcess;

public class OrleansTest<TSetup> : IClassFixture<TSetup>
    where TSetup : IntegrationTestSetup
{
    public OrleansTest(TSetup fixture)
    {
        Fixture = fixture;
        fixture.SetName(GetType().Name);
    }

    protected TSetup Fixture { get; }

}
