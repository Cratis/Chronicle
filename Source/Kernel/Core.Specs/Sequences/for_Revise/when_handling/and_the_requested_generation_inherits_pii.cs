// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_Revise.when_handling;

public class and_the_requested_generation_inherits_pii : and_the_request_contains_nested_pii
{
    protected override bool InheritsProtection => true;
}
