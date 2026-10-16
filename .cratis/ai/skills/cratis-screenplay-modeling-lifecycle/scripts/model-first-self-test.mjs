// cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/scripts/model-first-self-test.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, rmSync, symlinkSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
export function selfTest(modelFirstStatus) {
    const temporary = mkdtempSync(join(tmpdir(), 'model-first-self-test-'));
    let cases = 0;
    const write = (path, content) => {
        mkdirSync(dirname(path), { recursive: true });
        writeFileSync(path, content);
    };
    const git = (repository, args) => {
        const result = spawnSync('git', ['-c', 'user.name=Model first self-test', '-c', 'user.email=specs@example.invalid',
            '-c', 'commit.gpgsign=false', '-c', 'core.hooksPath=/dev/null', ...args], { cwd: repository, encoding: 'utf8', timeout: 10000 });
        assert.equal(result.status, 0, result.stderr || result.error?.message);
    };
    const repo = name => {
        const repository = join(temporary, name);
        mkdirSync(repository);
        git(repository, ['init', '--quiet']);
        return repository;
    };
    const check = (repository, expected, root) => {
        const report = modelFirstStatus(repository, root);
        assert.equal(report.optedIn, expected);
        cases++;
        return report;
    };
    try {
        const drafts = repo('drafts');
        check(drafts, false);
        write(join(drafts, 'Screenplay/model.play'), 'module Test\n');
        assert.equal(check(drafts, false).drafts.untracked.length, 1);
        git(drafts, ['add', 'Screenplay/model.play']);
        assert.equal(check(drafts, false).drafts.staged.length, 1);
        git(drafts, ['commit', '--quiet', '-m', 'Accept model']);
        assert.equal(check(drafts, true).contract, 'HEAD');
        write(join(drafts, 'Screenplay/model.play'), 'module Changed\n');
        assert.equal(check(drafts, true).modelChangesInProgress.length, 1);
        const configured = repo('configured');
        write(join(configured, '.cratis/ai.json'), JSON.stringify({ mcpServers: { screenplay: { root: 'Models' } } }));
        check(configured, true, 'Models');
        write(join(configured, 'Models/model.play'), 'module Test\n');
        assert.equal(check(configured, true, 'Models').committed.length, 0);
        const outside = repo('outside');
        write(join(outside, 'docs/example.play'), 'module Example\n');
        git(outside, ['add', '.']);
        git(outside, ['commit', '--quiet', '-m', 'Add example']);
        check(outside, false);
        assert.throws(() => modelFirstStatus(temporary), /git rev-parse/);
        cases++;
        write(join(configured, '.cratis/ai.json'), '{');
        assert.throws(() => modelFirstStatus(configured));
        cases++;
        for (const [name, path] of [
            ['fixture', 'Source/X.Specs/Projections/for_model/Cases/fixture.play'],
            ['example', 'Source/examples/example.play'],
            ['docs', 'src/docs/example.play'],
        ]) {
            const repository = repo(name);
            write(join(repository, path), 'module Example\n');
            git(repository, ['add', '.']);
            git(repository, ['commit', '--quiet', '-m', 'Add non-project model']);
            assert.equal(check(repository, false, undefined).modelFilesExamined, 0);
        }
        const project = repo('project');
        write(join(project, 'Source/model.play'), 'module Test\n');
        git(project, ['add', '.']);
        git(project, ['commit', '--quiet', '-m', 'Accept project model']);
        assert.equal(check(project, true, undefined).modelRoot, 'Source');
        write(join(project, '.cratis/screenplay/draft.play'), 'module Draft\n');
        assert.equal(check(project, true, undefined).modelRoot, 'Source');
        git(project, ['add', '.cratis/screenplay/draft.play']);
        assert.equal(check(project, true, undefined).modelRoot, 'Source');
        const broad = repo('broad');
        for (const path of ['Source/model.play', 'Other/model.play', 'Source/samples/example.play']) {
            write(join(broad, path), 'module Test\n');
        }
        git(broad, ['add', '.']);
        git(broad, ['commit', '--quiet', '-m', 'Add project and sample models']);
        const broadReport = check(broad, true, undefined);
        assert.equal(broadReport.modelRoot, '.');
        assert.equal(broadReport.committed.length, 2);
        write(join(broad, '.cratis/ai.json'), JSON.stringify({ mcpServers: { screenplay: { root: '.' } } }));
        assert.equal(check(broad, true, undefined).committed.length, 3);
        const aliasRepository = repo('alias');
        const alias = join(aliasRepository, 'skills');
        symlinkSync(resolve(dirname(fileURLToPath(import.meta.url)), '../..'), alias, 'dir');
        const result = spawnSync(process.execPath, [join(alias, 'cratis-screenplay-modeling-lifecycle/scripts/model-first-status.mjs'), '--json'], {
            cwd: aliasRepository, encoding: 'utf8', timeout: 20000,
        });
        assert.equal(result.status, 1, result.stderr);
        assert.equal(JSON.parse(result.stdout).optedIn, false);
        cases++;
        console.log(`model-first-status self-test: checked ${cases} fixtures/verdicts; passed`);
    } finally {
        rmSync(temporary, { recursive: true, force: true });
    }
}
