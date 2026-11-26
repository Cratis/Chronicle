// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

public class and_properties_are_sourced_from_the_event_source_id : Specification
{
    const string Source = """
    using System;
    using Cratis.Chronicle.Projections.ModelBound;

    namespace System.Runtime.CompilerServices
    {
        public sealed class IsExternalInit { }
    }

    namespace Cratis.Chronicle.Projections.ModelBound
    {
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class FromEventAttribute<TEvent> : Attribute { }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
        public sealed class SetFromEventSourceIdAttribute<TEvent> : Attribute { }
    }

    namespace Sample
    {
        public record Registered;

        [FromEvent<Registered>]
        public record Account(
            Guid Id,
            [SetFromEventSourceId<Registered>] Guid Source,
            [property: SetFromEventSourceId<Registered>] Guid PropertySource);
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(Source);

    [Fact] Task should_recognize_both_parameter_and_property_mappings() => _result;
}
