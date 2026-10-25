// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

public class when_disposing_owned_material : Specification
{
    KernelFixtureCertificate _material;
    bool _existedDuringStop;

    void Establish() => _material = KernelFixtureCertificate.Create();

    async Task Because() => await _material.DisposeAfter(() =>
    {
        _existedDuringStop = File.Exists(_material.Path);
        return ValueTask.CompletedTask;
    });

    [Fact] void should_stop_the_container_before_deleting_material() => _existedDuringStop.ShouldBeTrue();
    [Fact] void should_remove_the_key_after_stopping() => File.Exists(_material.Path).ShouldBeFalse();
    [Fact] void should_remove_the_owned_directory() => Directory.Exists(Path.GetDirectoryName(_material.Path)).ShouldBeFalse();
}
