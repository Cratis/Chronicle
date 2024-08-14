// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.OrleansInProcess;

public abstract class IntegrationTestSetup(GlobalFixture globalFixture) : OrleansFixture(globalFixture), IAsyncLifetime
{
    public virtual Task Establish()
    {
        return Task.CompletedTask;
    }
    public virtual Task Because()
    {
        return Task.CompletedTask;
    }

    public virtual Task Destroy()
    {
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        EnsureBuilt();
        await Establish();
        await Because();
    }

    public new Task DisposeAsync() => Destroy();
}
