// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.given;

public class DecisionLogger : ILogger<ComplianceAuthorization>
{
    public List<string> Values { get; } = [];
    public List<Exception?> Exceptions { get; } = [];
    public int Entries { get; private set; }
    public int InformationEntries { get; private set; }
    public int WarningEntries { get; private set; }

    public bool IsEnabled(LogLevel logLevel) => true;
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries++;
        if (logLevel == LogLevel.Information) InformationEntries++;
        if (logLevel == LogLevel.Warning) WarningEntries++;
        Values.Add(formatter(state, exception));
        Exceptions.Add(exception);
        if (state is IEnumerable<KeyValuePair<string, object?>> arguments)
        {
            Values.AddRange(arguments.Select(argument => $"{argument.Key}={argument.Value}"));
        }
    }
}
