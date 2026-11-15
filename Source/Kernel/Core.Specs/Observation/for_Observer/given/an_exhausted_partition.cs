// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_exhausted_partition : a_quarantined_partition
{
    /// <summary>
    /// Gets the number of failed attempts to count against the current budget.
    /// </summary>
    protected virtual int AttemptsInBudget => MaxRetryAttempts + 1;

    void Establish()
    {
        while (FailedPartition.AttemptsInCurrentBudget < AttemptsInBudget)
        {
            _failedPartitionsState.RegisterAttempt((Key)Partition, LastFailedSequenceNumber, ["retry failed"], string.Empty);
        }
    }
}
