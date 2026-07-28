// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.when_resolving_embedded;

/// <summary>
/// Constructing a <see cref="ManifestEmbeddedFileProvider"/> over an assembly without an embedded files
/// manifest throws, so the absence has to be detected up front rather than caught.
/// </summary>
public class and_the_assembly_has_nothing_embedded : Specification
{
    IFileProvider? _result;

    void Because() => _result = WorkbenchUI.ResolveEmbedded(typeof(and_the_assembly_has_nothing_embedded).Assembly, "Whatever.Files");

    [Fact] void should_not_resolve_a_provider() => _result.ShouldBeNull();
}
