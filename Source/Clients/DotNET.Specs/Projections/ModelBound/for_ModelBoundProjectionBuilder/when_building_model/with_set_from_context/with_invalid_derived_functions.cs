// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_context;

public class with_invalid_derived_functions : given.a_model_bound_projection_builder
{
    Exception? _parameterError;
    Exception? _propertyError;
    Exception? _fromEveryError;

    void Because()
    {
        _parameterError = Catch.Exception(() => builder.Build(typeof(UnknownFunction)));
        _propertyError = Catch.Exception(() => builder.Build(typeof(IncompatibleReceiver)));
        _fromEveryError = Catch.Exception(() => builder.Build(typeof(NonterminalFunction)));
    }

    [Fact] void should_reject_unknown_parameter_functions() => _parameterError.ShouldBeOfExactType<InvalidPropertyForType>();
    [Fact] void should_reject_incompatible_property_receivers() => _propertyError.ShouldBeOfExactType<InvalidPropertyForType>();
    [Fact] void should_reject_nonterminal_from_every_functions() => _fromEveryError.ShouldBeOfExactType<InvalidPropertyForType>();
    [Fact] void should_report_the_original_context_type() => _fromEveryError.Message.ShouldContain(typeof(EventContext).FullName);
    [Fact] void should_report_the_original_context_path() => _fromEveryError.Message.ShouldContain("Occurred.Week.Day");

    record UnknownFunction([Key] Guid Id, [SetFromContext<DebitAccountOpened>("Occurred.Unknown")] int Week);

    record IncompatibleReceiver
    {
        [Key]
        public Guid Id { get; init; }

        [SetFromContext<DebitAccountOpened>("SequenceNumber.Week")]
        public int Week { get; init; }
    }

    record NonterminalFunction([Key] Guid Id, [FromEvery(contextProperty: "Occurred.Week.Day")] int Week);
}
