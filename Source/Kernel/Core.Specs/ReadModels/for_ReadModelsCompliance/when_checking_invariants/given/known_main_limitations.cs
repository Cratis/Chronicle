// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;

public static class known_main_limitations
{
    public static bool Allows(string shape, bool erased, string member, string protection, string invariant, Exception? error)
    {
        // Main a89c5ed35 already drops undeclared recursive members and rejects undeclared nested members.
        // Protection and erasure invariants never have exemptions, including when main leaked plaintext.
        var nonNull = member is not "null" and not "nullable_null";
        return (shape, protection, invariant) switch
        {
            ("recursive", "undeclared", "I4_member") => nonNull,
            ("nested_object" or "object_array", "undeclared", "I4_nested") => nonNull && error is SchemaPropertyNotFoundInSchema,
            _ => false
        };
    }
}
