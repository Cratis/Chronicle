// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationAdmission.when_resetting_before_admission;

public class on_sqlite : given.a_sqlite_provider
{
    Exception _error;

    async Task Establish()
    {
        await Advance(2);
        await new SqlKernelStateResetHandler(OptionsForProvider, Provider).Reset();
    }

    async Task Because() => _error = await Catch.Exception(Admit);

    [Fact] void should_refuse_a_below_floor_binary() => _error.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] async Task should_keep_the_floor() => (await Provider.ReadGenerationFloors())[Name].Revision.ShouldEqual(1UL);
}
#endif
