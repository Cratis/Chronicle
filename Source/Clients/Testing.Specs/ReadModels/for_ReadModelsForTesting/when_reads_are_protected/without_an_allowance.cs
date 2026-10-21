// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_reads_are_protected;

public class without_an_allowance : given.protected_surfaces
{
    Task Because() => ExerciseModels();

    [Fact] void should_refuse_models_and_event_store_reads() => _errors.Values.ShouldEachConformTo(error => error is ReadRefusedWhileReadsAreProtected);
    [Fact] void should_cover_all_surfaces() => _errors.Count.ShouldEqual(13);
}
