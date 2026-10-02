// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;

public static class known_main_limitations
{
    public static bool Allows(string shape, bool erased, string member, string protection, string invariant, Exception? error)
    {
        // Characterized against origin/main a89c5ed35, not promises of supported behavior. These pre-existing
        // conversion/protection gaps are outside #4453; the erasure freeze and any new failure remain mandatory.
        // The raw matrix and these limitations are discussed in https://github.com/Cratis/Chronicle/pull/4454.
        var nonNull = member is not "null" and not "nullable_null";
        var scalar = nonNull && member != "value_object";
        return (shape, protection, invariant) switch
        {
            ("composed_member", "pii", "I1") => scalar,
            ("composed_member", "pii", "I2") => erased && scalar && member is not "nullable_string" and not "nullable_empty_string",
            ("composed_member", "pii" or "non_pii", "I3") => scalar,
            ("duplicate_member", "pii", "I1") => nonNull,
            ("duplicate_member", "pii", "I2") => erased && nonNull && member != "empty_string",
            ("referenced_member", "pii", "I1") => member is not "nullable_null" and not "value_object",
            ("referenced_member", "pii", "I2") => erased && member is not "nullable_null" and not "value_object" and not "empty_string",
            ("referenced_member", "pii" or "non_pii", "I3") => member == "date" || member == "decimal" || member == "null" || member == "referenced_enum",
            ("referenced_member", "non_pii", "I4_member") => member == "date" || member == "decimal" || member == "null",
            ("recursive", "undeclared", "I4_member") => nonNull,
            ("nested_object" or "object_array", "undeclared", "apply_or_release") => nonNull && error is SchemaPropertyNotFoundInSchema,
            ("composed_member" or "referenced_member" or "scalar_array", "pii", "apply_or_release") =>
                member == "value_object" && error is InvalidOperationException &&
                error.Message == "A value of type 'System.String' cannot be converted to a 'System.Int32'.",
            _ => false
        };
    }
}
