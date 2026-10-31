// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Observers.when_registering_a_renamed_reactor.Renamed;

public class SendWelcomeEmail : IReactor
{
    public Task Handle(SomeEvent @event) => Task.CompletedTask;
}
