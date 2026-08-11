// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// Only the primary constructor generates properties, so only its parameters are the second spelling of a member.
/// An ordinary secondary constructor generates nothing, and its parameters must not be paired with anything.
/// </summary>
/// <remarks>
/// Picking the constructor with the most parameters agrees with the primary one for a type that declares no other
/// - which is every other spec here, and is why this went unnoticed. Give the type a longer secondary constructor
/// and the heuristic pairs the declared <c>Stamp</c> off against that constructor's parameter, drops the property
/// from its own path, and re-finds its attributes through the parameter's name lookup. The finding survives and
/// the squiggle lands on an unattributed parameter of a constructor that maps nothing - a true positive pointing
/// at the wrong token, which is a worse failure than none: the author reads it, sees a bare parameter, and
/// concludes the rule is broken.
/// </remarks>
public class and_a_secondary_constructor_declares_more_parameters : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    public record AccountAudit(Guid Id)
    {
        public AccountAudit(Guid id, DateTimeOffset Stamp)
            : this(id)
        {
        }

        [SetFromContext<AccountOpened>("SequenceNumber")]
        [SetFromContext<AccountOpened>("Occurred")]
        public DateTimeOffset {|#0:Stamp|} { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Stamp", "AccountOpened"));

    [Fact] Task should_report_the_discarded_mapping_on_the_property() => _result;
}
