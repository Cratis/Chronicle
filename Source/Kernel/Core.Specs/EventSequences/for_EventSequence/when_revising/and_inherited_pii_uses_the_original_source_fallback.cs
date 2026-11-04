// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_inherited_pii_uses_the_original_source_fallback : and_the_requested_generation_inherits_pii
{
    protected override Subject? OriginalSubject => null;
}
