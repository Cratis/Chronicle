// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey.given;

public sealed record foreign_key : SequencingKey
{
    public foreign_key(SequencingKey original) : base(original)
    {
    }
}
