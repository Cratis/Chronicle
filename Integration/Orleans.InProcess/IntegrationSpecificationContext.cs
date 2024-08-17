// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

public abstract class IntegrationSpecificationContext(GlobalFixture globalFixture) : OrleansFixture(globalFixture), IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        EnsureBuilt();
        await OnEstablish();
        await OnBecause();
    }

    public new Task DisposeAsync() => OnDestroy();


    Task OnEstablish()
    {
        return InvokeMethod("Establish");
    }

    Task OnBecause()
    {
        return InvokeMethod("Because");
    }

    Task OnDestroy()
    {
        return InvokeMethod("Destroy");
    }


    Task InvokeMethod(string name)
    {
#nullable disable
        return typeof(SpecificationMethods<,>).MakeGenericType(GetType(), typeof(Specification)).GetMethod(name, BindingFlags.Static | BindingFlags.Public).Invoke(null, [this]) as Task;
    }
}
