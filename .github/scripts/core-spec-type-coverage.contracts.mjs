// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, rm } from 'node:fs/promises';
import path from 'node:path';
import { coreExclusionFixture } from './core-spec-fixture.mjs';
import { discoverTypes, discoverSpecClasses } from './core-spec-source.mjs';
import { coverageTypes, validateExclusions, exclusionPath } from './core-spec-coverage.mjs';
import { CORE_PROJECT, checkPartition } from './core-spec-partition.mjs';

async function fixture(t, source) {
    const scratch = path.resolve('.ai-work/core-split/fixtures');
    await mkdir(scratch, { recursive: true });
    const root = await mkdtemp(path.join(scratch, 'type-guard-'));
    t.after(() => rm(root, { recursive: true, force: true }));
    await coreExclusionFixture(root);
    await writeFile(path.join(root, path.dirname(CORE_PROJECT), 'Types.cs'), source);
    return root;
}
const partition = prefixes => ({ version: 1, units: prefixes.map(prefix => ({ prefix, weight: 1 })) });
const otherTypes = 'namespace B { class Helper {} } namespace C { record Helper; } namespace D { record struct Helper; }';

test('nested sibling and nested-generic bases resolve for reporting without controlling type coverage', async t => {
    const root = await fixture(t, `namespace Demo.Covered {
        class Outer {
            abstract class Base { [Fact] public void should_work() {} }
            class when_x : Base {}
        }
        abstract class Base<T> { [Theory] public void should_work() {} }
        class when_y : Base<Dictionary<string, List<int>>> {}
        record Helper;
    } ${otherTypes}`);
    assert.deepEqual((await discoverSpecClasses(root)).map(info => info.fqn), ['Demo.Covered.Outer+when_x', 'Demo.Covered.when_y']);
    const types = await coverageTypes(root);
    for (const name of ['Outer', 'Outer+Base', 'Outer+when_x', 'Base`1', 'when_y', 'Helper']) {
        assert.ok(types.some(info => info.fqn === `Demo.Covered.${name}`));
    }
    await checkPartition(root, partition(['Demo.Covered.', 'B.', 'C.', 'D.']));
});

test('guard rejects an uncovered nested type even if it has no detected test methods', async t => {
    const root = await fixture(t, `namespace Demo { class Outer { class when_x : UnknownExternalBase {} } } ${otherTypes}`);
    const types = await coverageTypes(root);
    assert.ok(types.some(info => info.fqn === 'Demo.Outer+when_x'));
    // A class-specific term covers Outer, but must NOT stand in for its nested types.
    await assert.rejects(checkPartition(root, partition(['Demo.Outer.', 'B.', 'C.', 'D.'])), /Unassigned Core type Demo\.Outer\+when_x/);
});

test('guard rejects uncovered records, abstract types and test-free nested generic derivatives', async t => {
    for (const declaration of ['record Uncovered;', 'abstract class Uncovered {}', 'class Uncovered : Base<Dictionary<string, List<int>>> {}']) {
        const root = await fixture(t, `namespace Demo { ${declaration} } ${otherTypes}`);
        await assert.rejects(checkPartition(root, partition(['Unused.', 'B.', 'C.', 'D.'])), /Unassigned Core type Demo.Uncovered/);
    }
});

test('excluded helpers are explicit, stale-checked, and cannot acquire tests or Specification-style bases', async t => {
    for (const declaration of [
        'class Helper { [Fact] void fact() {} }',
        'record Helper { [Theory] void theory() {} }',
        'class Helper { void should_work() {} }',
        'class Helper : Specification {}',
        'class Helper : CommandScenario<Command> {}',
        'class Helper { [CustomFact] void custom_test() {} }',
        'abstract class Base { [Fact] void inherited_test() {} } class Helper : Base {}'
    ]) {
        const root = await fixture(t, `namespace Demo { ${declaration} }`);
        const entries = [{ fqn: 'Demo.Helper', reason: 'Test helper' }];
        assert.throws(() => validateExclusions([], entries), /Stale/);
        assert.throws(() => validateExclusions([], [{ fqn: 'Demo.Helper' }]), /Invalid/);
        const types = await discoverTypes(root);
        assert.throws(() => validateExclusions(types, entries), /test signals|Specification-style/);
    }
});

test('by-name exclusion closure follows nested, generic and aliased derivatives transitively', async t => {
    for (const source of [
        'class Helper {} class Child : Helper { [Fact] void fact() {} }',
        'class Helper {} class Child : Helper {} class Grandchild : Child { [Theory] void theory() {} }',
        'class Helper<T> {} class Child : Helper<Dictionary<string, List<int>>> { [Fact] void fact() {} }',
        'class Helper {} class Outer { class Child : Helper, Specification {} }',
        'class Helper {} class Outer { class Child : Helper { void should_work() {} } }'
    ]) {
        const root = await fixture(t, `namespace Demo { ${source} }`);
        const types = await discoverTypes(root);
        const helper = types.find(info => info.namespace === 'Demo' && info.name === 'Helper');
        assert.throws(() => validateExclusions(types, [{ fqn: helper.fqn, reason: 'Helper' }]), /by-name derivative/);
    }
    const root = await fixture(t, 'using Alias = Demo.Helper; namespace Demo { class Helper {} class Child : Alias { [Fact] void fact() {} } }');
    const types = await discoverTypes(root);
    assert.throws(() => validateExclusions(types, [{ fqn: 'Demo.Helper', reason: 'Helper' }]), /by-name derivative/);
});

test('unsupported declarations fail closed instead of disappearing from coverage', async t => {
    for (const source of ['namespace Demo { class Ω {} }', 'namespace Demo { delegate void Callback(); }']) {
        const root = await fixture(t, source);
        await assert.rejects(coverageTypes(root), /Unsupported/);
    }
});

test('partial declarations cannot hide unsafe exclusions and stale entries fail the real guard', async t => {
    const root = await fixture(t, 'namespace Demo { partial class Helper {} partial class Helper { [Fact] void fact() {} } }');
    const types = await discoverTypes(root);
    assert.throws(() => validateExclusions(types, [{ fqn: 'Demo.Helper', reason: 'Helper' }]), /test signals/);
    await writeFile(path.join(root, exclusionPath), JSON.stringify([{ fqn: 'Demo.Removed', reason: 'Removed helper' }]));
    await assert.rejects(coverageTypes(root), /Stale helper exclusion/);
});
