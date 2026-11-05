// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_the_layout_cannot_be_proven;

public class when_proving_it : Specification
{
    readonly List<string> _verified = [];
    readonly List<string> _inspectedUsable = [];

    async Task Because()
    {
        foreach (var step in Enum.GetValues<UnprovableLayouts.Step>())
        {
            foreach (var (name, create) in UnprovableLayouts.Failures)
            {
                var (client, _) = UnprovableLayouts.Create(step, create());
                var check = new VaultKeyStoreUpgradeCheck(client);
                if (!(await check.EnsureLayout()).LayoutUnusable)
                {
                    _verified.Add($"{step}/{name}/ensure");
                }

                if (!(await check.Inspect(true)).LayoutUnusable)
                {
                    _inspectedUsable.Add($"{step}/{name}/inspect");
                }
            }
        }
    }

    [Fact] void should_not_verify_the_layout_for_any_failure() => _verified.ShouldBeEmpty();
    [Fact] void should_not_report_a_usable_layout_when_inspecting_for_any_failure() => _inspectedUsable.ShouldBeEmpty();
}
