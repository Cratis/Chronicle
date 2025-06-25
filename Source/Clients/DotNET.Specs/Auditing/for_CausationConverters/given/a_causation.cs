// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters.given;

public class a_causation : Specification
{
    protected DateTimeOffset _occurred;
    protected CausationType _type;
    protected IDictionary<string, string> _properties;
    protected Causation _causation;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _type = CausationType.Root;
        _properties = new Dictionary<string, string> { { "key", "value" } };
        _causation = new(_occurred, _type, _properties);
    }
}
