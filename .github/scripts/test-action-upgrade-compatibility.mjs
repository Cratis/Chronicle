// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { readdirSync, readFileSync } from 'node:fs';

const workflows = new URL('../workflows/', import.meta.url);
const files = readdirSync(workflows).filter(file => /\.ya?ml$/.test(file));

function actionSteps(action) {
    const usage = new RegExp(`^ {8}uses: ${action.replaceAll('/', '\\/')}@`, 'm');
    const steps = files.flatMap(file => readFileSync(new URL(file, workflows), 'utf8')
        .split(/^ {6}- /m)
        .filter(step => usage.test(step))
        .map(step => ({ file, step })));
    assert.ok(steps.length > 0, `Expected uses of ${action}`);
    return steps;
}

test('Node setup leaves dependency caching to the existing explicit cache steps', () => {
    for (const { file, step } of actionSteps('actions/setup-node')) {
        assert.match(step, /^ {10}package-manager-cache: false$/m, file);
    }
});

test('Java setup keeps compiler and exception annotations enabled', () => {
    for (const { file, step } of actionSteps('actions/setup-java')) {
        assert.match(step, /^ {10}problem-matcher: true$/m, file);
    }
});

test('prerelease comments use the authenticated token input, not the removed environment contract', () => {
    for (const { step } of actionSteps('mshick/add-pr-comment')) {
        assert.match(step, /^ {10}repo-token: \$\{\{ secrets\.GITHUB_TOKEN \}\}$/m);
        assert.doesNotMatch(step, /^ {8}env:|allow-repeats:/m);
    }
});

test('NuGet and Docker prerelease comments remain distinct for each published version', () => {
    const comments = actionSteps('mshick/add-pr-comment');
    assert.equal(comments.length, 2);
    for (const kind of ['NuGet packages', 'Docker image']) {
        const { step } = comments.find(comment => comment.step.startsWith(`name: Add comment for ${kind}\n`));
        const prefix = kind === 'NuGet packages' ? 'nuget' : 'docker';
        assert.ok(step.includes(`message-id: ${prefix}-\${{ needs.release.outputs.version }}`));
    }
});
