// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_client_gets_disconnected : given.all_dependencies
{
    bool _registeredAfterDisconnect;

    void Establish()
    {
        // Set _registered to true via reflection to simulate a prior registration
        var registeredField = typeof(Reactors).GetField("_registered", BindingFlags.NonPublic | BindingFlags.Instance);
        registeredField?.SetValue(_reactors, true);
    }

    void Because()
    {
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();

        var registeredField = typeof(Reactors).GetField("_registered", BindingFlags.NonPublic | BindingFlags.Instance);
        _registeredAfterDisconnect = (bool)registeredField?.GetValue(_reactors)!;
    }

    [Fact] void should_reset_registered_flag() => _registeredAfterDisconnect.ShouldBeFalse();
}
