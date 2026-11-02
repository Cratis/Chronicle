// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.when_definition_changes_during_a_read.context;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection;

/// <summary>
/// Verifies the projection notification round trip through real Orleans scheduling.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.DefinitionChangeInterleaveCollection.Name)]
public class when_definition_changes_during_a_read(context context) : Given<context>(context)
{
    /// <summary>
    /// Runs the definition change while an immediate read is awaiting a fold.
    /// </summary>
    /// <param name="fixture">The isolated integration fixture.</param>
    public class context(given.DefinitionChangeInterleaveFixture fixture) : given.a_projection(fixture)
    {
        /// <summary>
        /// Gets whether SetDefinition acknowledged its subscribers before the fold was released.
        /// </summary>
        public bool ChangedBeforeRelease { get; private set; }

        /// <summary>
        /// Gets the name from the read which was active during the change.
        /// </summary>
        public string InFlightName { get; private set; }

        /// <summary>
        /// Gets the initial seed from the subsequent read.
        /// </summary>
        public string SubsequentSeed { get; private set; }

        async Task Because()
        {
            var read = _immediateProjection.GetModelInstance();
            try
            {
                await _gate.Entered.WaitAsync(TimeSpan.FromSeconds(30));
                await _changeDefinition().WaitAsync(TimeSpan.FromSeconds(30));
                ChangedBeforeRelease = true;
            }
            finally
            {
                _gate.Release();
            }

            var inFlight = await read.WaitAsync(TimeSpan.FromSeconds(30));
            InFlightName = inFlight.ReadModel["name"]!.GetValue<string>();
            var subsequent = await _immediateProjection.GetModelInstance().WaitAsync(TimeSpan.FromSeconds(30));
            SubsequentSeed = subsequent.ReadModel["seed"]!.GetValue<string>();
        }
    }

    [Fact] void should_acknowledge_the_definition_before_releasing_the_fold() => Context.ChangedBeforeRelease.ShouldBeTrue();
    [Fact] void should_finish_the_in_flight_read_without_losing_its_model() => Context.InFlightName.ShouldEqual("projected");
    [Fact] void should_use_the_changed_definition_on_the_next_read() => Context.SubsequentSeed.ShouldEqual("changed");
}
