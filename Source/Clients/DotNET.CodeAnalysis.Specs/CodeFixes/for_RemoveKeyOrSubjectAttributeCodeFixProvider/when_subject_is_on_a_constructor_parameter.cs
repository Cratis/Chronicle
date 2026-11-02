// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveKeyOrSubjectAttributeCodeFixProvider;

public class when_subject_is_on_a_constructor_parameter : given.a_subject_attribute_code_fix_context
{
    Task Because() => RegisterFixForSubject("""
        public record OrderId(Guid Value) : EventSourceId<Guid>(Value);
        public record ActorId(Guid Value) : EventSourceId<Guid>(Value);
        public record OrderPlaced(OrderId Id, [Subject] ActorId Actor);
        """);

    [Fact] void should_not_offer_to_remove_the_subject_even_for_a_stale_diagnostic() => _actions.ShouldBeEmpty();
}
