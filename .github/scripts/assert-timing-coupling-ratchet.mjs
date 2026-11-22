// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Per-file sleep counts may only decrease. Integration Task.Delay calls additionally need
// a nonempty `// Task.Delay: <why>` on the same or preceding line. --report prints counts
// in baseline format; --self-test exercises the real CLI with an intentionally bad file.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const baselinePath = '.github/timing-coupling-baseline.txt';
const rule = 'the Cratis specification-by-example guidance (github.com/Cratis/AI)';
const excluded = new Set(['bin', 'obj', 'node_modules']);
const sleeps = /\b(?:Thread\.Sleep|Task\.Delay|SpinWait|Task\.Yield)\b/g;
const delays = /\bTask\.Delay\b/g;
const explanation = /\/\/ Task\.Delay:\s*\S/;
const normalize = path => path.replaceAll('\\', '/').replace(/^\.\//, '');

function inScope(path) {
    if (path.includes('.claude/worktrees/')) return false;
    return path.startsWith('Integration/') || path.startsWith('Source/Clients/XUnit.Integration/') ||
        path.split('/').some(segment => segment.endsWith('.Specs'));
}

function sourceFiles(root, directory) {
    const files = [];
    for (const entry of readdirSync(join(root, directory), { withFileTypes: true })) {
        const path = normalize(join(directory, entry.name));
        if (entry.isDirectory()) {
            if (!excluded.has(entry.name)) files.push(...sourceFiles(root, path));
        }
        if (entry.isFile() && path.endsWith('.cs') && inScope(path)) files.push(path);
    }
    return files;
}

function sitesIn(text) {
    const lines = text.split(/\r?\n/);
    const found = [];
    const undocumented = [];
    let delayOccurrences = 0;
    let inComment = false;
    for (const [index, line] of lines.entries()) {
        // Preserve the original ratchet's line-based scope while ignoring block comments too.
        const code = line.replace(/\/\*[\s\S]*?\*\//g, '').split('//')[0];
        if (inComment) {
            inComment = !line.includes('*/');
            continue;
        }
        inComment = code.includes('/*');
        const active = code.split('/*')[0];
        found.push(...Array.from(active.matchAll(sleeps), () => index + 1));
        const delayCount = Array.from(active.matchAll(delays)).length;
        delayOccurrences += delayCount;
        const documented = explanation.test(line) || explanation.test(lines[index - 1] ?? '');
        if (delayCount && !documented) {
            undocumented.push(...Array(delayCount).fill(index + 1));
        }
    }
    return { found, undocumented, delayOccurrences };
}

function scan(root) {
    const files = ['Source', 'Integration'].flatMap(directory => sourceFiles(root, directory));
    assert(files.length > 0, 'No in-scope C# files found; refusing an empty scan.');
    const integration = files.filter(path => path.startsWith('Integration/'));
    assert(integration.length > 0, 'No Integration C# files found; refusing an empty comment check.');
    return new Map(files.sort().map(path => [path, sitesIn(readFileSync(join(root, path), 'utf8'))]));
}

function baseline(root) {
    if (!existsSync(join(root, baselinePath))) {
        throw new Error(`::error::${baselinePath} is missing. The ratchet cannot run without its baseline.`);
    }
    const allowed = new Map();
    for (const line of readFileSync(join(root, baselinePath), 'utf8').split(/\r?\n/)) {
        const entry = line.split('#')[0].trim();
        if (!entry) continue;
        const match = /^(\d+)\s+(.+)$/.exec(entry);
        assert(match, `Malformed baseline entry: ${line}`);
        allowed.set(normalize(match[2].trim()), Number(match[1]));
    }
    assert(allowed.size > 0, 'Empty sleep baseline; refusing a vacuous ratchet.');
    return allowed;
}

function error(path, message) {
    console.error(`::error file=${path}::${message}`);
}

function checkCounts(onDisk, allowed) {
    let failed = false;
    for (const [path, { found }] of onDisk) {
        if (found.length <= (allowed.get(path) ?? 0)) continue;
        failed = true;
        error(path, countFailure(path, found, allowed));
    }
    for (const [path, count] of allowed) {
        const current = onDisk.get(path)?.found.length ?? 0;
        if (current < count) {
            const remaining = current ? `lower its entry to ${current}` : `delete its entry from ${baselinePath}`;
            console.log(`::notice file=${path}::${path} now waits on the clock ${current} time(s) instead of ${count} - ${remaining}.`);
        }
    }
    return failed;
}

function countFailure(path, found, allowed) {
    const lines = found.join(', ');
    if (!allowed.has(path)) {
        return `${path} is not in ${baselinePath} and waits on the clock ${found.length} time(s), on line(s) ${lines}. A spec ` +
            `awaits a fact, not a duration - see ${rule}. If the delay is genuinely not a wait ` +
            '(a test double that is slow on purpose, an infrastructure readiness backoff, or a ' +
            '`Task.Delay(1)` widening an interleaving window), add the entry to ' + baselinePath + ' and state which one it is in the pull request.';
    }
    return `${path} waits on the clock ${found.length} time(s) but ${baselinePath} allows ${allowed.get(path)}. The new site(s) are among line(s) ` +
        `${lines}. Baseline entries may only decrease - await a fact instead, see ${rule}.`;
}

function run(root, report) {
    const onDisk = scan(root);
    if (report) {
        for (const [path, { found }] of onDisk) if (found.length) console.log(`${found.length} ${path}`);
        return 0;
    }
    const allowed = baseline(root);
    console.log(`Ratchet examined ${onDisk.size} C# files and ${Array.from(onDisk.values()).reduce((sum, sites) => sum + sites.found.length, 0)} sleep occurrences.`);
    const countsFailed = checkCounts(onDisk, allowed);
    const commentsFailed = checkComments(onDisk);
    return countsFailed || commentsFailed ? 1 : 0;
}

function checkComments(onDisk) {
    let failed = false;
    let files = 0;
    let occurrences = 0;
    for (const [path, sites] of onDisk) {
        if (!path.startsWith('Integration/')) continue;
        files++;
        occurrences += sites.delayOccurrences;
        for (const line of sites.undocumented) {
            failed = true;
            error(path, `Task.Delay at line ${line} needs // Task.Delay: <why> on the same or preceding line.`);
        }
    }
    console.log(`Comment check examined ${files} Integration C# files and ${occurrences} Task.Delay occurrences.`);
    return failed;
}

function selfTest(root) {
    const work = join(root, '.ai-work');
    mkdirSync(work, { recursive: true });
    const fixture = mkdtempSync(join(work, 'timing-ratchet-self-test-'));
    const plant = (path, text) => {
        mkdirSync(dirname(join(fixture, path)), { recursive: true });
        writeFileSync(join(fixture, path), text);
    };
    const cli = () => spawnSync(process.execPath, [fileURLToPath(import.meta.url), fixture], { encoding: 'utf8' });
    try {
        plant(baselinePath, '1 Integration/planted.cs\n1 Source/Example.Specs/spec.cs\n');
        plant('Source/Example.Specs/spec.cs', 'await Task.Yield();');
        plant('Integration/planted.cs', 'await Task.Delay(500);');
        const violation = cli();
        assert.equal(violation.status, 1, violation.stdout + violation.stderr);
        assert.match(violation.stderr, /Task.Delay at line 1 needs/);
        plant('Integration/planted.cs', '// Task.Delay: intentional quiet window\nawait Task.Delay(500);');
        const passing = cli();
        assert.equal(passing.status, 0, passing.stdout + passing.stderr);
        plant('Integration/planted.cs', 'await Task.Delay(500); // Task.Delay: intentional quiet window');
        assert.equal(cli().status, 0);
        plant('Integration/planted.cs', '// Task.Delay:\nawait Task.Delay(500);');
        assert.equal(cli().status, 1, 'Empty explanations must fail.');
        plant('Integration/planted.cs', '// Task.Delay: reason\n\nawait Task.Delay(500);');
        assert.equal(cli().status, 1, 'Nonadjacent explanations must fail.');
        plant('Integration/planted.cs', '// Task.Delay: intentional quiet window\nawait Task.Delay(500);');
        plant('Source/Example.Specs/spec.cs', 'await Task.Yield();\nawait Task.Yield();');
        const increased = cli();
        assert.equal(increased.status, 1, 'The existing per-file count ratchet must still reject increases.');
        assert.match(increased.stderr, /waits on the clock 2 time\(s\).*allows 1/);
        assert.deepEqual(sitesIn('// Task.Delay(1);\n/* Task.Delay(2); */').found, []);
        console.log('Self-test passed: planted violation rejected, adjacent explanations accepted, count ratchet retained.');
        return 0;
    } finally {
        rmSync(fixture, { recursive: true });
    }
}

try {
    const args = process.argv.slice(2);
    assert(args.every(arg => !arg.startsWith('-') || ['--report', '--self-test'].includes(arg)), 'Unknown option.');
    const roots = args.filter(arg => !arg.startsWith('-'));
    assert(roots.length <= 1, 'Expected at most one repository root.');
    const root = resolve(roots[0] ?? '.');
    assert(existsSync(join(root, 'Integration')), 'Repository root has no Integration directory.');
    process.exitCode = args.includes('--self-test') ? selfTest(root) : run(root, args.includes('--report'));
} catch (error) {
    console.error(error.message);
    process.exitCode = 1;
}
