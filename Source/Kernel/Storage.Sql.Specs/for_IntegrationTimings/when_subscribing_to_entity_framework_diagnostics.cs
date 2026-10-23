// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_IntegrationTimings;

public class when_subscribing_to_entity_framework_diagnostics : Specification
{
    readonly List<string> _lines = [];
    readonly string[] _commandEvents =
    [
        RelationalEventId.CommandExecuting.Name,
        RelationalEventId.CommandExecuted.Name,
        RelationalEventId.CommandError.Name,
        RelationalEventId.CommandCanceled.Name
    ];
    bool _otherEnabled;
    bool _connectionEnabled;
    bool _allCommandsEnabled;

    void Because()
    {
        using var observer = CommandEvents.NewObserver(_lines, out _);
        using var listener = new DiagnosticListener("Microsoft.EntityFrameworkCore");
        observer.OnNext(listener);
        _allCommandsEnabled = _commandEvents.All(name => listener.IsEnabled(name));
        _connectionEnabled = listener.IsEnabled(RelationalEventId.ConnectionOpening.Name);
        _otherEnabled = listener.IsEnabled("Some.Other.Event");
    }

    [Fact] void should_enable_the_four_command_events() => _allCommandsEnabled.ShouldBeTrue();
    [Fact] void should_not_enable_connection_events() => _connectionEnabled.ShouldBeFalse();
    [Fact] void should_not_enable_unrelated_events() => _otherEnabled.ShouldBeFalse();
}
