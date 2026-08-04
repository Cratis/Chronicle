// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// A reactor is mostly ordinary helpers, and an <c>async Task</c> one is the commonest shape there is. Deciding
/// handler-versus-helper on the return type made every one of them a handler: its first parameter was demanded
/// to be an event type, and its ordinary value-type arguments were reported as dependencies that could not be
/// resolved. A helper returning a string or a domain record escaped, purely because its return type fell outside
/// the supported set - so which helpers were flagged came down to what they happened to return.
/// </summary>
/// <remarks>
/// The discriminator is the first parameter, which is what Chronicle's own discovery uses - over public and
/// non-public instance methods alike, so accessibility is not it either: a private handler really is discovered.
/// </remarks>
public class and_the_method_is_a_private_helper : given.a_reactor_method_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class TimesheetDue
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public Task On(TimesheetDue @event) => Send("a-consultant", true);

        async Task Send(string consultant, bool overdue)
        {
            await Task.CompletedTask;
        }

        async Task<string> Describe(string consultant, int month)
        {
            await Task.CompletedTask;
            return consultant;
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
