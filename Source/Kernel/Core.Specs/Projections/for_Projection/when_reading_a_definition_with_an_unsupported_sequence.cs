// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_Projection;

public class when_reading_a_definition_with_an_unsupported_sequence : given.a_pinned_projection_grain
{
    readonly List<Exception> _errors = [];

    async Task Because()
    {
        var model = for_ProjectionsManager.given.a_projections_manager_grain.CreateReadModelDefinition(_definition.ReadModel);
        foreach (var owner in Enum.GetValues<ProjectionOwner>().Where(_ => _ != ProjectionOwner.Kernel))
        {
            foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
            {
                await _grain.SetDefinition(_definition with { Owner = owner, EventSequenceId = sequence });
                _errors.Add(await Catch.Exception(() => _grain.GetEventTypes()));
                _errors.Add(await Catch.Exception(() => _grain.GetEventTypesForPreview(model)));
            }
        }
    }

    [Fact] void should_refuse_both_read_paths() => _errors.All(_ => _ is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_not_compile_a_definition() => _engineDefinition.ShouldBeNull();
}
