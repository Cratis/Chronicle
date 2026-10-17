// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_using_an_effect_receipt;

public class with_a_response_at_the_limit : given.a_receipted_unit
{
    void Establish() => _unitOfWork.AddEvent(EventSequenceId.Log, "source", "created", Causation.Unknown());

    async Task Because()
    {
        _unitOfWork.UseEffectReceipt(_key, JsonValue.Create(new string('a', (64 * 1024) - 2)));
        await _unitOfWork.Commit();
    }

    [Fact] void should_accept_exactly_sixty_four_kibibytes_of_json() => _sentReceipt.Response.Length.ShouldEqual(64 * 1024);
}
