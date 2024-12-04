// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event.and_it_fails.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event;

[Collection(GlobalCollection.Name)]
public class and_it_fails(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_reactor_observing_an_event(globalFixture)
    {
    }
}
