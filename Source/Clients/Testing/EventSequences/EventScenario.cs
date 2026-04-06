// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a lightweight, in-process scenario for testing <see cref="IEventSequence"/> operations without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Use the <see cref="Given"/> property to seed pre-existing events into the in-memory event log before
/// exercising production code via <see cref="EventSequence"/> or <see cref="EventLog"/>.
/// </para>
/// <para>
/// Create a new <see cref="EventScenario"/> instance per test to keep tests isolated; the in-memory
/// event log accumulates state across calls on the same instance.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var scenario = new EventScenario();
/// await scenario.Given
///     .ForEventSource(myId)
///     .Events(new SomeEvent("value"), new OtherEvent("other"));
/// var result = await scenario.EventLog.Append(myId, new AnotherEvent("more"));
/// result.ShouldBeSuccessful();
/// </code>
/// </para>
/// </remarks>
/// <param name="defaults">The <see cref="Defaults"/> to use for service resolution.</param>
public class EventScenario(Defaults defaults)
{
    readonly InMemoryEventLog _eventLog = new(defaults.EventTypes);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class using <see cref="Defaults.Instance"/>.
    /// </summary>
    public EventScenario()
        : this(Defaults.Instance)
    {
    }

    /// <summary>
    /// Gets the fluent builder used to seed pre-existing events into the event log before the act phase.
    /// </summary>
    public EventScenarioGivenBuilder Given => new(_eventLog, defaults.EventTypes);

    /// <summary>
    /// Gets the in-memory <see cref="IEventLog"/> that can be used to perform <c>Append</c> and <c>AppendMany</c> operations.
    /// </summary>
    public IEventLog EventLog => _eventLog;

    /// <summary>
    /// Gets the in-memory <see cref="IEventSequence"/> that can be used to perform <c>Append</c> and <c>AppendMany</c> operations.
    /// </summary>
    /// <remarks>
    /// This is the same underlying instance as <see cref="EventLog"/>.
    /// </remarks>
    public IEventSequence EventSequence => _eventLog;
}
