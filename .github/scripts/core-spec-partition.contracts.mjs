// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, rm } from 'node:fs/promises';
import path from 'node:path';
import { CORE_PROJECT, CORE_PARTS, coreUnitId, discoverSpecClasses, discoverNamespaces, loadPartition,
    partitionUnits, checkPartition, assertExactOnce, matchesFilter, partFilter, refresh } from './core-spec-partition.mjs';
import { planShards } from './spec-shards.mjs';
import { coreSpecFixture, coreExclusionFixture } from './core-spec-fixture.mjs';
import { coverageTypes } from './core-spec-coverage.mjs';
import { unitInfos, invocation } from './run-mixed-specs.mjs';

const unit = (prefix, weight = 1) => ({ prefix, weight });
const data = units => ({ version: 1, units });
const simple = ['A.', 'B.', 'C.', 'D.'].map(prefix => unit(prefix));
async function fixture(t, source) {
    const scratch = path.resolve('.ai-work/core-split/fixtures');
    await mkdir(scratch, { recursive: true });
    const root = await mkdtemp(path.join(scratch, 'specs-'));
    t.after(() => rm(root, { recursive: true, force: true }));
    const directory = path.join(root, path.dirname(CORE_PROJECT));
    await mkdir(directory, { recursive: true });
    await coreExclusionFixture(root);
    if (source) await writeFile(path.join(directory, 'Specs.cs'), source);
    return root;
}

test('Core source guard covers every non-excluded type, independent of spec discovery', async () => {
    const classes = await discoverSpecClasses(process.cwd());
    assert.ok(classes.length > 7000);
    assert.ok(classes.every(info => info.namespace !== 'ThirdParty' && info.namespace !== 'Cratis.Chronicle'));
    const parts = await checkPartition(process.cwd(), await loadPartition());
    assert.equal(parts.length, CORE_PARTS);
    const types = await coverageTypes(process.cwd());
    assert.ok(types.length > classes.length);
    assert.ok(types.some(info => info.fqn.includes('+')));
    assertExactOnce(types.map(info => info.fqn), parts);
    assert.ok(parts.every(part => part.units.length && part.weight > 0));
});

test('source discovery includes inherited facts/theories and excludes abstract bases and helpers', async t => {
    const root = await fixture(t, `
        namespace Demo.Base {
            public abstract class facts { [Fact] public void should_work() {} }
            public class helper { string text = "[Fact] public class fake {}"; }
        }
        namespace Demo.Specs {
            using Demo.Base;
            using alias = Demo.Base.facts;
            public class inherited : facts {}
            public class aliased : alias {}
            public class theory { [SomeOther, Xunit.Theory] void should_work() {} }
            // [Fact] class imaginary {}
            public class helper {}
        }`);
    assert.deepEqual((await discoverSpecClasses(root)).map(info => info.fqn), ['Demo.Specs.aliased', 'Demo.Specs.inherited', 'Demo.Specs.theory']);
    assert.deepEqual(await discoverNamespaces(root), ['Demo.Specs']);
    await mkdir(path.join(root, path.dirname(CORE_PROJECT), 'obj'));
    await writeFile(path.join(root, path.dirname(CORE_PROJECT), 'obj', 'Ignored.cs'), 'namespace Bad; public class ignored { [Fact] void x() {} }');
    assert.equal((await discoverSpecClasses(root)).length, 3);
});

test('exact-once guard fails for synthetic uncovered and doubly-covered spec classes', () => {
    const parts = partitionUnits(simple);
    assertExactOnce(['A.spec', 'B.spec', 'C.spec', 'D.spec'], parts);
    assert.throws(() => assertExactOnce(['Uncovered.spec'], parts), /matches 0 parts/);
    const overlap = structuredClone(parts);
    overlap[1].units.push(unit('A.spec.'));
    assert.throws(() => assertExactOnce(['A.spec'], overlap), /matches 2 parts/);
    // The adapter uses substring, not startsWith: a middle-of-name collision must fail too.
    assert.throws(() => assertExactOnce(['X.A.spec'], [...parts, { units: [unit('X.A.')] }]), /matches 2 parts/);
});

test('trailing dots prevent sibling collisions and class terms avoid capturing child namespaces', () => {
    assert.equal(matchesFilter('Demo.Specs.sibling_extra', ['Demo.Specs.sibling.']), false);
    const parts = [{ units: [unit('Demo.Parent.own_spec.')] }, { units: [unit('Demo.Parent.Child.')] }];
    assertExactOnce(['Demo.Parent.own_spec', 'Demo.Parent.Child.spec'], parts);
    assert.equal(partFilter(['Demo.A.', 'Demo.B.']), '(FullyQualifiedName~Demo.A.|FullyQualifiedName~Demo.B.)');
    for (const prefixes of [[], ['Demo.A'], ['Demo.A.|Name~oops']]) assert.throws(() => partFilter(prefixes), /filter/);
});

test('LPT is deterministic with ordinal ties and rejects empty parts', () => {
    const expected = partitionUnits([...simple, unit('E.', 2)]);
    assert.deepEqual(partitionUnits([...simple, unit('E.', 2)].reverse()), expected);
    assert.deepEqual(expected.map(part => part.weight), [3, 3]);
    assert.throws(() => partitionUnits(simple.slice(0, CORE_PARTS - 1)), /Empty Core part/);
    assert.throws(() => partitionUnits(simple, CORE_PARTS + 1), /Expected 2/);
});

test('partition check fails closed for new specs, missing units, malformed weights and overlaps', async t => {
    const root = await fixture(t);
    await coreSpecFixture(root);
    const valid = await loadPartition();
    await checkPartition(root, valid);
    for (const weight of [0, -1, null, '1', Infinity, NaN]) {
        const invalid = structuredClone(valid);
        invalid.units[0].weight = weight;
        await assert.rejects(checkPartition(root, invalid), /weight/);
    }
    for (const prefix of [valid.units[0].prefix, valid.units[0].prefix + 'Child.']) {
        await assert.rejects(checkPartition(root, data([...valid.units, unit(prefix)])), /overlapping/);
    }
    await assert.rejects(checkPartition(root, data([...valid.units, unit('Unused.Namespace.')])), /Unmatched/);
    await writeFile(path.join(root, path.dirname(CORE_PROJECT), 'NewSpec.cs'), 'namespace BrandNew.Area; public class spec { [Fact] void should_work() {} }');
    await assert.rejects(checkPartition(root, valid), /Unassigned.*--refresh/);
});

test('planner expands Core once into configured unit identities and receipts cannot treat them as the whole project', async () => {
    const projects = [CORE_PROJECT, 'Source/A/A.csproj', 'Source/B/B.csproj', 'Source/C/C.csproj'];
    const timings = { version: 1, defaultSeconds: 1, measurements: [], core: { fixedSeconds: 55, variableSeconds: 100 } };
    const plan = planShards(projects, timings, partitionUnits(simple));
    const ids = plan.flatMap(shard => shard.projects);
    assert.equal(ids.length, CORE_PARTS + 3);
    assert.equal(ids.includes(CORE_PROJECT), false);
    for (let part = 1; part <= CORE_PARTS; part++) assert.equal(ids.filter(id => id === coreUnitId(part)).length, 1);
    assert.equal(plan.reduce((sum, shard) => sum + shard.seconds, 0), 100 + CORE_PARTS * 55 + 3);
    const infos = unitInfos(ids, projects.map(project => ({ project, runner: 'v3', dll: '/Core.dll' })), partitionUnits(simple));
    const core = infos.filter(info => info.corePart);
    assert.equal(core.length, CORE_PARTS);
    for (const info of core) {
        assert.equal(info.project, CORE_PROJECT);
        const call = invocation(info, { directory: `/out/part-${info.corePart}`, filter: `(ordinary)&${info.partFilter}`, timeout: 100 });
        assert.ok(call.args.at(-1).includes('FullyQualifiedName~'));
        assert.ok(call.args.at(-1).includes('/Core.dll'));
    }
});

test('refresh is reproducible, weights fact durations plus overhead and rejects unknown TRX classes', async t => {
    const root = await fixture(t);
    await writeFile(path.join(root, path.dirname(CORE_PROJECT), 'Specs.cs'), simple.map((_, index) => `namespace Demo.Area${index} { public class spec { [Fact] void should_work() {} } }`).join('\n'));
    const trx = path.join(root, 'results.trx');
    const xml = names => `<TestRun><Results>${names.map((_, index) => `<UnitTestResult testId="${index}" duration="00:00:01" />`).join('')}</Results><TestDefinitions>${names.map((fqn, index) => `<UnitTest id="${index}"><TestMethod className="${fqn}" /></UnitTest>`).join('')}</TestDefinitions></TestRun>`;
    const names = simple.map((_, index) => `Demo.Area${index}.spec`);
    await writeFile(trx, xml(names));
    const generated = await refresh(root, [trx]);
    assert.deepEqual(await refresh(root, [trx]), generated);
    assert.ok(generated.units.every(entry => entry.weight === 0.25));
    await writeFile(trx, xml(['Not.Discovered.spec']));
    await assert.rejects(refresh(root, [trx]), /not discovered/);
});
