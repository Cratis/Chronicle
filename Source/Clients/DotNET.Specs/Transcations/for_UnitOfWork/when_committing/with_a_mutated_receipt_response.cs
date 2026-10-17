// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_a_mutated_receipt_response : given.a_receipted_unit
{
    void Establish()
    {
        var response = new JsonObject { ["value"] = "original" };
        _unitOfWork.UseEffectReceipt(_key, response);
        response["value"] = "mutated";
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", "created", Causation.Unknown());
    }

    Task Because() => _unitOfWork.Commit();

    [Fact] void should_commit_the_enrollment_snapshot() => _sentReceipt.Response.ShouldEqual("{\"value\":\"original\"}");
}
