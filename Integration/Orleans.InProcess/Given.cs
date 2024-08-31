// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

public class Given<TSetup> : IClassFixture<TSetup>
    where TSetup : IntegrationSpecificationContext
{
    public Given(TSetup context)
    {
        Context = context;
        context.SetName(GetType().FullName);
    }

    public TSetup Context { get; }
}
