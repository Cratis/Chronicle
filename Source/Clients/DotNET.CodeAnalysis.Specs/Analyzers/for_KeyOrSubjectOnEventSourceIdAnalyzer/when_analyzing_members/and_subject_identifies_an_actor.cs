// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyOrSubjectOnEventSourceIdAnalyzer.when_analyzing_members;

public class and_subject_identifies_an_actor : given.a_key_or_subject_on_event_source_id_analyzer
{
    string _source;
    Task _result;

    void Establish() => _source = CreateSource("""
        public record OrderId(Guid Value) : EventSourceId<Guid>(Value);
        public record ActorId(Guid Value) : EventSourceId<Guid>(Value);

        public record OrderPlaced(OrderId Id, [Subject] ActorId Actor);
        public record OrderApproved(OrderId Id, [property: Subject] ActorId Actor);
        """);

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyOrSubjectOnEventSourceIdAnalyzer>.VerifyAnalyzer(_source);

    [Fact] Task should_allow_an_actor_subject_on_properties_and_constructor_parameters() => _result;
}
