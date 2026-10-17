// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.given;

public class an_occurrence_storage_with_a_retrying_execution_strategy : an_occurrence_storage
{
    protected override bool UsesARetryingExecutionStrategy => true;
}
