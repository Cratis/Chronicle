// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_reads_are_protected;

public class with_advisory_reads : given.protected_surfaces
{
    Task Because() => ExerciseLog(ReadAllowance.Advisory);

    [Fact] void should_allow_every_log_read_family() => _errors.Values.ShouldEachConformTo(error => error is null);
    [Fact] void should_cover_all_overloads() => _errors.Count.ShouldEqual(12);
}
