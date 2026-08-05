// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Projections.for_Projections;

/// <summary>
/// A model-bound projection is declared by attributes on the read model and has no type of its own, so the read model
/// is the only handle a caller holds for it.
/// </summary>
/// <remarks>
/// Asking for state took a type constrained to <c>IProjection</c>, which a read model does not implement - so there
/// was no way to ask a model-bound projection for its state at all, while failed partitions for the same projection
/// were reachable. The handler behind both is the same one; only the surface was missing.
/// </remarks>
public class when_asking_for_the_state_of_a_model_bound_projection : given.a_discovered_model_bound_projection
{
    ProjectionState _byModelType;
    ProjectionState _byModelTypeArgument;

    void Establish() =>
        _observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>())
            .Returns(new ObserverInformation
            {
                RunningState = ObserverRunningState.Active,
                IsSubscribed = true,
                NextEventSequenceNumber = 43,
                LastHandledEventSequenceNumber = 42,
                TailEventSequenceNumber = 42
            });

    async Task Because()
    {
        _byModelType = await _projections.GetStateForModel<TheModelBoundReadModel>();
#pragma warning disable CA2263 // the untyped overload is the thing under test here
        _byModelTypeArgument = await _projections.GetStateForModel(typeof(TheModelBoundReadModel));
#pragma warning restore CA2263
    }

    [Fact] void should_ask_the_observer_for_that_projection() =>
        _observers.Received().GetObserverInformation(Arg.Is<GetObserverInformationRequest>(_ => _.ObserverId == _projections.GetProjectionIdForModel<TheModelBoundReadModel>().Value));

    [Fact] void should_report_the_state() => _byModelType.IsSubscribed.ShouldBeTrue();
    [Fact] void should_answer_the_same_through_the_type_argument() => _byModelTypeArgument.NextEventSequenceNumber.ShouldEqual(_byModelType.NextEventSequenceNumber);
}
