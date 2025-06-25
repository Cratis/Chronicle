// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters.given;

public class a_causation : Specification
{
    protected DateTimeOffset occurred;
    protected CausationType type;
    protected IDictionary<string, string> properties;
    protected Causation causation;

    void Establish()
    {
        occurred = DateTimeOffset.UtcNow;
        type = CausationType.Root;
        properties = new Dictionary<string, string> { { "key", "value" } };
        causation = new(occurred, type, properties);
    }
}
