// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_acknowledged_append_without_receipts : an_acknowledged_append
{
    protected override bool IncludeReceipts => false;
}
