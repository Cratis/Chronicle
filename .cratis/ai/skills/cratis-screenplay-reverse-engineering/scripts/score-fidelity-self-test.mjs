// cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/scripts/score-fidelity-self-test.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';

export function selfTest(score, CatalogError) {
    let checks = 0;
    const near = (actual, expected) => {
        assert.ok(Math.abs(actual - expected) < 1e-9, `${actual} != ${expected}`);
        checks++;
    };
    const item = (id, cls, status, extra = {}) => ({ id, class: cls, status, ...extra });
    const weights = { rule: 0.5, rejection: 0.5 };
    const catalog = items => ({ weights, items });

    // All expressed scores 1.0.
    near(score(catalog([item('a', 'rule', 'expressed'), item('b', 'rejection', 'expressed')])).behavior.behaviorModel, 1);

    // Mixed partial and missing: rule (1 + 0) / 2 = 0.5, rejection 0.5 -> 0.5.
    near(score(catalog([item('a', 'rule', 'expressed'), item('b', 'rule', 'missing'), item('c', 'rejection', 'partial')])).behavior.behaviorModel, 0.5);

    // An executed fail overrides expressed and is listed; not-run falls back to the model status.
    const executed = score(catalog([item('a', 'rule', 'expressed', { executed: 'fail' }), item('b', 'rejection', 'expressed', { executed: 'not-run' })])).behavior;
    near(executed.behaviorExecuted, 0.5);
    near(executed.behaviorModel, 1);
    assert.deepEqual(executed.executedFailedButExpressed, ['a']);
    near(executed.notRun, 1);

    // Item weights: time counts 1.5 by default, so 1.5 / 2.5 of the class when the other item is missing.
    const timed = score({ weights: { rule: 0.5, time: 0.5 }, items: [item('a', 'rule', 'missing'), item('b', 'time', 'expressed'), item('c', 'time', 'missing', { weight: 1.5 })] }).behavior;
    near(timed.byClass.time.model, 0.5);
    near(timed.behaviorModel, 0.25);

    // Naming: leaked items are excluded from numerator and denominator.
    const naming = { items: [
        { category: 'commands', name: 'A', result: 'exact', leaked: true },
        { category: 'commands', name: 'B', result: 'missing', leaked: false },
        { category: 'events', name: 'C', result: 'renamed', leaked: false }
    ] };
    const named = score(catalog([item('a', 'rule', 'expressed'), item('b', 'rejection', 'expressed')]), naming).naming;
    near(named.namingAll, (0.2 * 0.5 + 0.15 * 0) / 0.35);
    near(named.namingNonLeaked, 0);
    near(named.leakShare, 1 / 3);
    near(named.legacyStructure, (0.2 * 0.5 + 0.15 * 1) / 0.35);

    // Legacy structure arithmetic: normalized over the structure categories present.
    const legacy = { items: [
        ...Array.from({ length: 25 }, (_, index) => ({ category: 'commands', name: `c${index}`, result: index < 23 ? 'exact' : 'missing' })),
        { category: 'events', name: 'e', result: 'partial' },
        { category: 'readModels', name: 'r', result: 'exact' },
        { category: 'screens', name: 's', result: 'exact' },
        { category: 'concepts', name: 'k', result: 'exact' },
        { category: 'constraints', name: 'x', result: 'missing' }
    ] };
    const base = score(catalog([item('a', 'rule', 'expressed'), item('b', 'rejection', 'expressed')]), legacy).naming;
    near(base.legacyStructure, (0.2 * 0.92 + 0.15 * 0.5 + 0.15 + 0.15 + 0.1) / 0.75);
    near(base.legacyRules, 0);

    // Invalid input exits 2 (CatalogError).
    const bad = [
        { weights: { rule: 0.6, rejection: 0.6 }, items: [item('a', 'rule', 'expressed'), item('b', 'rejection', 'expressed')] },
        catalog([item('a', 'rule', 'maybe'), item('b', 'rejection', 'expressed')]),
        catalog([item('a', 'rule', 'expressed')]),
        catalog([item('a', 'flow', 'expressed'), item('b', 'rejection', 'expressed')]),
        catalog([item('a', 'rule', 'expressed'), item('a', 'rejection', 'expressed')]),
        { weights, items: [] }
    ];
    for (const entry of bad) {
        assert.throws(() => score(entry), CatalogError);
        checks++;
    }
    assert.throws(() => score(catalog([item('a', 'rule', 'expressed'), item('b', 'rejection', 'expressed')]), { items: [{ category: 'commands', name: 'x', result: 'bogus' }] }), CatalogError);
    checks++;

    console.log(`${checks} checks passed`);
}
