// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;

namespace Cratis.Chronicle.Chaos.for_ContainerControl.given;

/// <summary>
/// Supplies a pure Docker-command stand-in without starting processes or containers.
/// </summary>
public class a_container_control : Specification
{
    /// <summary>The command runner responses.</summary>
    protected Queue<Func<Task<string>>> _responses;

    /// <summary>The requested command argument lists.</summary>
    protected List<IReadOnlyList<string>> _commands;

    /// <summary>The recorded schedule log.</summary>
    protected List<string> _log;

    /// <summary>The state-aware control under test.</summary>
    private protected ContainerControl _control;

    /// <summary>A running container snapshot.</summary>
    private protected ContainerFaultState _running;

    /// <summary>A stopped container snapshot with exit diagnostics.</summary>
    private protected ContainerFaultState _down;

    void Establish()
    {
        _responses = [];
        _commands = [];
        _log = [];
        _running = new() { Status = "running", Running = true };
        _down = new() { Status = "exited", ExitCode = 134, Error = "process exited", FinishedAt = "2026-11-10T11:44:08Z" };
        _control = new(Run, _log.Add);
    }

    /// <summary>Adds a projected state response.</summary>
    /// <param name="state">The observed container state.</param>
    private protected void State(ContainerFaultState state) => _responses.Enqueue(() => Task.FromResult(JsonSerializer.Serialize(state)));

    /// <summary>Adds a control failure response.</summary>
    /// <param name="error">The command error.</param>
    protected void Fail(Exception error) => _responses.Enqueue(() => Task.FromException<string>(error));

    Task<string> Run(IReadOnlyList<string> arguments, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        _commands.Add(arguments);
        return _responses.Dequeue()();
    }
}
