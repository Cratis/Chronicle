// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Chronicle.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation.given;

public class a_fallback_enum_generator : when_generating_a_shared_type.given.a_shared_type_generator
{
    protected XElement _documentation = null!;

    protected void Generate()
    {
        var code = _generator.GenerateSharedType(typeof(DocumentedFallbackStatus), _outputDir);
        var lines = code.Split('\n').Where(line => line.StartsWith("/// ", StringComparison.Ordinal)).Select(line => line[4..]);
        _documentation = XElement.Parse($"<member>{string.Join('\n', lines)}</member>");
    }
}
