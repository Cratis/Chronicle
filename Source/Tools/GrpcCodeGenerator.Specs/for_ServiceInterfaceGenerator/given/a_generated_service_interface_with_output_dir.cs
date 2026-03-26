// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.given;

/// <summary>
/// Base context that creates a generator and a unique temporary output directory.
/// </summary>
public class a_generated_service_interface_with_output_dir : a_generated_service_interface
{
    protected string _outputDir = null!;

    void Establish()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_outputDir);
    }
}
