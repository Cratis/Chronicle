// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_using_an_effect_receipt;

public class with_a_non_ascii_response_over_the_limit : given.a_receipted_unit
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _unitOfWork.UseEffectReceipt(_key, JsonValue.Create(new string('é', 32 * 1024))));

    [Fact] void should_limit_the_encoded_json_not_character_count() => _error.ShouldBeOfExactType<EffectReceiptResponseTooLarge>();
}
