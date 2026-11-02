// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.MongoDB.Integration.Setup.for_ReleasedAdmissionCompatibility;

/// <summary>
/// Closure-negative checks only. No serializer, filter or retry behaviour is exercised here.
/// </summary>
public class when_resolving_assemblies_outside_the_image_closure : Cratis.Specifications.Specification
{
    string _root;
    string _first;
    string _second;
    string _overlapping;
    AdmissionLoadContext _context;
    Exception _unresolvedError;
    Exception _hostBclError;
    Exception _hostExtensionsError;
    Exception _hostOrleansError;
    Exception _openClosureError;
    Exception _conflictError;
    Exception _overlapError;
    Exception _missingRootError;
    Exception _escapeError;
    Exception _sharedTypeError;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"chronicle-5518-closure-{Guid.NewGuid():N}");
        _first = Path.Combine(_root, "first");
        _second = Path.Combine(_root, "second");
        _overlapping = Path.Combine(_first, "nested");
        Directory.CreateDirectory(_overlapping);
        Directory.CreateDirectory(_second);

        // Only one managed host assembly is copied into the image; its references are deliberately absent.
        var hostAssembly = typeof(ChronicleOptions).Assembly.Location;
        File.Copy(hostAssembly, Path.Combine(_first, Path.GetFileName(hostAssembly)));

        // Same native asset name with different bytes in two roots is a conflict.
        File.WriteAllText(Path.Combine(_first, "native.dll"), "first");
        File.WriteAllText(Path.Combine(_second, "native.dll"), "second");

        _context = new("closure-negative", [_first]);
    }

    void Because()
    {
        _unresolvedError = Catch.Exception(() => _context.LoadFromAssemblyName(new AssemblyName("Cratis.Chronicle.Closure.Missing")));
        _hostBclError = Catch.Exception(() => _context.LoadFromAssemblyName(new AssemblyName("System.Net.Http")));
        _hostExtensionsError = Catch.Exception(() => _context.LoadFromAssemblyName(new AssemblyName("Microsoft.Extensions.Primitives")));
        _hostOrleansError = Catch.Exception(() => _context.LoadFromAssemblyName(typeof(IGrainFactory).Assembly.GetName()));
        _openClosureError = Catch.Exception(() => _context.EnsureClosed(typeof(ChronicleOptions).Assembly.GetName().Name!));
        _conflictError = Catch.Exception(() => new AdmissionLoadContext("conflict", [_first, _second]).Unload());
        _overlapError = Catch.Exception(() => new AdmissionLoadContext("overlap", [_first, _overlapping]).Unload());
        _missingRootError = Catch.Exception(() => new AdmissionLoadContext("missing", [Path.Combine(_root, "absent")]).Unload());
        _escapeError = Catch.Exception(() => _context.EnsureContained(typeof(ChronicleOptions)));
        _sharedTypeError = Catch.Exception(() => _context.EnsureContained(typeof(object)));
    }

    [Fact] void should_fail_closed_for_unresolved_assembly() => _unresolvedError.ShouldBeOfExactType<FileNotFoundException>();
    [Fact] void should_not_share_bcl_like_name_outside_the_allowlist() => _hostBclError.ShouldBeOfExactType<FileNotFoundException>();
    [Fact] void should_not_share_microsoft_extensions_from_host() => _hostExtensionsError.ShouldBeOfExactType<FileNotFoundException>();
    [Fact] void should_not_fall_back_to_host_orleans() => _hostOrleansError.ShouldBeOfExactType<FileNotFoundException>();
    [Fact] void should_not_allowlist_microsoft_extensions() => AdmissionLoadContext.SharedBcl.Any(name => name.StartsWith("Microsoft.", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_report_unresolved_references_of_an_open_closure() => _openClosureError.ShouldBeOfExactType<FileNotFoundException>();
    [Fact] void should_reject_conflicting_assets_across_roots() => _conflictError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_overlapping_roots_as_ambiguous() => _overlapError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_missing_root() => _missingRootError.ShouldBeOfExactType<DirectoryNotFoundException>();
    [Fact] void should_detect_type_escaping_to_the_host_context() => _escapeError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_accept_shared_bcl_type() => _sharedTypeError.ShouldBeNull();

    void Destroy()
    {
        _context.Unload();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
