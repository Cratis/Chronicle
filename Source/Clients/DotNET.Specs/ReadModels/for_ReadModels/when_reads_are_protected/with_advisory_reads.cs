// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_reads_are_protected;

public class with_advisory_reads : given.protected_read_surfaces
{
    Task Because() => Exercise(ReadAllowance.Advisory);

    [Fact] void should_admit_every_read_to_ordinary_model_validation() => _errors.Values.ShouldEachConformTo(error => error is UnknownReadModel);
}
