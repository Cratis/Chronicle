// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.when_resolving;

/// <summary>
/// A Kernel-only deployment ships no Workbench at all - that is a supported shape, not a failure, so
/// resolution reports the absence and the caller keeps the server running without the UI.
/// </summary>
public class and_the_ui_is_nowhere : Specification
{
    IFileProvider? _result;

    void Because() => _result = WorkbenchUI.Resolve(null, given.a_file_provider.Empty());

    [Fact] void should_not_resolve_a_provider() => _result.ShouldBeNull();
}
