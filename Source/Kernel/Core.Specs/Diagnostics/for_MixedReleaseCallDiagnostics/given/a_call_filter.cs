// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Releases;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics.given;

public class a_call_filter : Specification
{
    protected IClusterReleases _releases;
    protected IOutgoingGrainCallContext _context;
    private protected MixedReleaseCallDiagnostics _filter;
    protected List<string> _messages = [];
    protected List<Exception?> _loggedExceptions = [];
    protected Exception _original = new TypeLoadException("hostile-exception-content-{Payload}");
    protected Exception? _failure;
    protected bool _throwFromLogger;

    void Establish()
    {
        _releases = Substitute.For<IClusterReleases>();
        _releases.Summarize().Returns(new ClusterReleaseSummary([(new(19, 111, 0), 1), (new(19, 112, 0), 1)], 0, 0));
        _context = Substitute.For<IOutgoingGrainCallContext>();
        _context.InterfaceMethod.Returns(typeof(IPatchManager).GetMethod(nameof(IPatchManager.ApplyPatchesFor)));
        _context.Invoke().Returns(_ => Task.FromException(_original));
        _filter = new(_releases, new RecordingLogger(this));
    }

    protected async Task Invoke() => _failure = await Catch.Exception(() => _filter.Invoke(_context));

    sealed class RecordingLogger(a_call_filter owner) : ILogger<MixedReleaseCallDiagnostics>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (owner._throwFromLogger) throw new InvalidOperationException("logger failed");
            owner._messages.Add(formatter(state, exception));
            owner._loggedExceptions.Add(exception);
        }
    }
}
