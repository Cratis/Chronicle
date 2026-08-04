// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DeclarativeProjectionAnalyzer.when_analyzing_projection_builder_methods;

/// <summary>
/// A builder method's other type parameters are the child read-model type of <c>Children</c>, the key type of
/// <c>IdentifiedBy</c> and the join-key property type of <c>On</c>. None of them is an event, none of them could
/// be, and demanding <c>[EventType]</c> on them reported correct code at every such call site - as an error, and
/// pointing at a call with no visible type argument at all, because these are always inferred.
/// </summary>
public class and_the_type_argument_is_not_in_an_event_position : given.a_declarative_projection_analyzer_with_the_whole_builder_surface
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class SomethingHappened
    {
        public string Reference { get; set; }
    }

    public class Line
    {
        public string Id { get; set; }
    }

    public class Projection
    {
        readonly Cratis.Chronicle.Projections.IProjectionBuilderFor<Projection> _builder;

        public string Reference { get; set; }
        public IEnumerable<Line> Lines { get; set; }

        public Projection(Cratis.Chronicle.Projections.IProjectionBuilderFor<Projection> builder)
        {
            _builder = builder;
        }

        public void Build()
        {
            _builder.From<SomethingHappened>();
            _builder.Children(_ => _.Lines, children => children.IdentifiedBy(_ => _.Id));
            _builder.Join<SomethingHappened>(join => join.On(_ => _.Reference));
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DeclarativeProjectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
