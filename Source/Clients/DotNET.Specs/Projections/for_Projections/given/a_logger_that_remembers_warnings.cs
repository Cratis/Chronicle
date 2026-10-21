// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.for_Projections.given;

/// <summary>
/// An <see cref="ILogger{Projections}"/> that keeps the warnings it was given, so a spec can assert on what was said.
/// </summary>
public class a_logger_that_remembers_warnings : ILogger<Projections>
{
    readonly List<string> _warnings = [];

    /// <summary>Gets the messages of the warnings logged so far.</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning)
        {
            _warnings.Add(formatter(state, exception));
        }
    }
}
