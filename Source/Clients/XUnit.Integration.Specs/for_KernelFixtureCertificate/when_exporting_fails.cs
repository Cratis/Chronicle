// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

[UnsupportedOSPlatform("windows")]
public class when_exporting_fails : Specification
{
    readonly IOException _failure = new("export failed");
    Exception? _result;
    string _path;
    UnixFileMode _directoryMode;
    UnixFileMode _fileMode;

    void Because() => _result = Catch.Exception(() => KernelFixtureCertificate.Create(exportCertificate: (_, stream) =>
    {
        _path = stream.Name;
        _directoryMode = File.GetUnixFileMode(Path.GetDirectoryName(_path)!);
        _fileMode = File.GetUnixFileMode(_path);
        stream.WriteByte(1);
        throw _failure;
    }));

    [Fact] void should_keep_the_export_failure() => _result.ShouldEqual(_failure);
    [Fact] void should_remove_partially_written_material() => File.Exists(_path).ShouldBeFalse();
    [Fact] void should_remove_the_owned_directory() => Directory.Exists(Path.GetDirectoryName(_path)).ShouldBeFalse();
    [Fact] void should_restrict_the_directory_before_export() => _directoryMode.ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    [Fact] void should_restrict_the_file_before_export() => _fileMode.ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
}
