// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Jobs.Placement.for_JobStepPlacementProperties.given;

public class a_provider : Specification
{
    protected ChronicleOptions _options;
    protected JobStepPlacementProperties _provider;
    protected Dictionary<string, string> _properties;

    void Establish()
    {
        _options = new();
        _properties = new();
        _provider = new(Options.Create(_options));
    }
}
