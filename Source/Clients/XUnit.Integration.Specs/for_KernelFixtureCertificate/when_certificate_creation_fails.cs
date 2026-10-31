// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

public class when_certificate_creation_fails : Specification
{
    readonly IOException _failure = new("certificate creation failed");
    Exception? _result;
    string _directory;

    void Because() => _result = Catch.Exception(() => KernelFixtureCertificate.Create(createCertificate: directory =>
    {
        _directory = directory;
        throw _failure;
    }));

    [Fact] void should_keep_the_creation_failure() => _result.ShouldEqual(_failure);
    [Fact] void should_remove_the_owned_directory() => Directory.Exists(_directory).ShouldBeFalse();
}
