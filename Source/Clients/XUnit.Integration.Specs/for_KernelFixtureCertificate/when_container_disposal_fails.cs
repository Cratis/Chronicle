// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

public class when_container_disposal_fails : Specification
{
    readonly IOException _failure = new("container disposal failed");
    KernelFixtureCertificate _material;
    Exception? _result;

    void Establish() => _material = KernelFixtureCertificate.Create();

    async Task Because() => _result = await Catch.Exception(async () => await _material.DisposeAfter(() => ValueTask.FromException(_failure)));

    async Task Destroy() => await _material.DisposeAfter(() => ValueTask.CompletedTask);

    [Fact] void should_preserve_the_disposal_failure() => _result.ShouldEqual(_failure);
    [Fact] void should_retain_material_while_stopping_is_unconfirmed() => File.Exists(_material.Path).ShouldBeTrue();
}
