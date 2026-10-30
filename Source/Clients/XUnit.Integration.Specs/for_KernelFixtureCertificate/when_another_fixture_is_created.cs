// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

public class when_another_fixture_is_created : Specification
{
    KernelFixtureCertificate _first;
    KernelFixtureCertificate _second;

    void Establish() => _first = KernelFixtureCertificate.Create();

    void Because() => _second = KernelFixtureCertificate.Create();

    async Task Destroy()
    {
        if (_first is not null) await _first.DisposeAfter(() => ValueTask.CompletedTask);
        if (_second is not null) await _second.DisposeAfter(() => ValueTask.CompletedTask);
    }

    [Fact] void should_not_share_material_paths() => _first.Path.ShouldNotEqual(_second.Path);
    [Fact] void should_not_share_private_keys() => _first.Hash.ShouldNotEqual(_second.Hash);
}
