// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer;

public class when_analyzing_async_void_handlers : given.a_reactor_method_analyzer
{
    [Theory]
    [InlineData("", "")]
    [InlineData(", Cratis.Chronicle.Events.EventContext context", "")]
    [InlineData(", IService service", "")]
    [InlineData("", "[Cratis.Chronicle.Reactors.OnceOnly]")]
    [InlineData("", "[Cratis.Chronicle.Reactors.Replay]")]
    public Task should_warn_on_the_handler_identifier(string dependencies, string attribute) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource($$"""
            [EventType] public class Happened { }
            public interface IService { }
            public class Reactor : Cratis.Chronicle.Reactors.IReactor
            {
                {{attribute}}
                public async void {|#0:On|}(Happened @event{{dependencies}}) { await Task.Yield(); }
            }
            """), new ExpectedDiagnostic("CHR0083", DiagnosticSeverity.Warning, "On"));

    [Fact]
    public Task should_recognize_explicit_event_and_reactor_registration() =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource("""
            public class Happened { }
            public class Reactor
            {
                public async void {|#0:On|}(Happened @event) { await Task.Yield(); }
                public void Configure()
                {
                    var artifacts = new Cratis.Chronicle.Registrations.ExplicitArtifacts();
                    artifacts.RegisterEventType<Happened>();
                    artifacts.RegisterReactor<Reactor>();
                }
            }
            """) + """

            namespace Cratis.Chronicle.Registrations
            {
                public class ExplicitArtifacts
                {
                    public void RegisterEventType<T>() { }
                    public void RegisterReactor<T>() { }
                }
            }
            """, new ExpectedDiagnostic("CHR0083", DiagnosticSeverity.Warning, "On"));
}
