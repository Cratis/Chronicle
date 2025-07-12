// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public static class CausationHelpers
{
    public static Causation New() =>
        new(DateTimeOffset.Now, "TestCausation", new Dictionary<string, string>()
            {
                { "TestKey", "TestValue" }
            });
}
