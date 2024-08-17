// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

public class OrleansTest<TSetup> : IClassFixture<TSetup>
    where TSetup : IntegrationTestSetup
{
    public OrleansTest(TSetup context)
    {
        Context = context;
        context.SetName(GetType().Name);
    }

    public TSetup Context { get; }
}
