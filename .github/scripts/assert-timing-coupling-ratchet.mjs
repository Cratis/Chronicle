// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Per-file sleep counts may only decrease. Integration Task.Delay calls additionally need
// a nonempty `// Task.Delay: <why>` on the same or preceding line. --report prints counts
// in baseline format; --self-test exercises the real CLI with an intentionally bad file.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { delimiter, dirname, join, resolve } from 'node:path';
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

function hide(active, start, end) {
    for (let index = start; index < end; index++) {
        if (!['\r', '\n'].includes(active[index])) active[index] = ' ';
    }
}

function interpolationEnd(text, token, closing, depth) {
    if (!closing) return 0;
    if (depth !== 0) return 0;
    if (text.startsWith(closing, token.index)) return token.index + closing.length;
    if (token[0] !== ':') return 0;
    const close = text.indexOf(closing, token.index + 1);
    return close < 0 ? text.length : close + closing.length;
}

function scanCode(text, active, start = 0, closing = '') {
    const tokens = /\/\/[^\r\n]*|\/\*[\s\S]*?(?:\*\/|$)|(?:\$+@?|@\$?)?("{3,}|"|')|::|[{}()[\]:]/g;
    tokens.lastIndex = start;
    let depth = 0;
    for (let token; (token = tokens.exec(text));) {
        const [value] = token;
        const end = interpolationEnd(text, token, closing, depth);
        if (end) {
            hide(active, token.index, end);
            return end;
        }
        if (['{', '(', '['].includes(value)) depth++;
        else if (['}', ')', ']'].includes(value)) depth--;
        else if (token[1]) tokens.lastIndex = scanString(text, active, token);
        else hide(active, token.index, tokens.lastIndex);
    }
    return text.length;
}

function quoteWidth(text, index, quote, verbatim) {
    if (quote.length >= 3) return text.startsWith(quote, index) ? quote.length : 0;
    if (text[index] === '\\' && !verbatim) return -2;
    if (text[index] !== quote) return 0;
    if (verbatim && text[index + 1] === '"') return -2;
    return 1;
}

function interpolationWidth(text, index, braces, raw) {
    if (!braces || text[index] !== '{') return 0;
    if (!raw) return text.startsWith('{{', index) ? -2 : 1;
    const run = /^\{+/.exec(text.slice(index))[0].length;
    return run >= braces ? run : -run;
}

function scanString(text, active, token) {
    const [opening, quote] = token;
    const raw = quote.length >= 3;
    const verbatim = opening.includes('@');
    const braces = opening.match(/\$/g)?.length ?? 0;
    let segment = token.index;
    let index = segment + opening.length;
    while (index < text.length) {
        const width = quoteWidth(text, index, quote, verbatim);
        if (width > 0) {
            hide(active, segment, index + width);
            return index + width;
        }
        if (width < 0) {
            index -= width;
            continue;
        }
        const interpolation = interpolationWidth(text, index, braces, raw);
        if (interpolation > 0) {
            hide(active, segment, index + interpolation);
            index = scanCode(text, active, index + interpolation, '}'.repeat(braces));
            segment = index;
        } else index += Math.abs(interpolation) || 1;
    }
    hide(active, segment, text.length);
    return text.length;
}

function codeOnly(text) {
    // Blank comments and literal text without shifting line numbers; interpolation expressions stay code.
    const active = text.split('');
    scanCode(text, active);
    return active.join('');
}

function sitesIn(text) {
    const lines = text.split(/\r?\n/);
    const found = [];
    const undocumented = [];
    let delayOccurrences = 0;
    for (const [index, active] of codeOnly(text).split(/\r?\n/).entries()) {
        const line = lines[index];
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

function testLexing(plant, cli) {
    const cases = [
        ['closed multiline comment', '/*\ncomment\n*/ await Task.Delay(123);', [3]],
        ['multiple comments', '/* first */ /*\nsecond */ await Task.Delay(123); /* third */ await Task.Yield();', [2, 2]],
        ['regular string', String.raw`var value = "https://example.test/* Task.Delay(1) */ \"quoted\""; await Task.Delay(123);`, [1]],
        ['verbatim string', 'var value = @"https://example.test/""quoted""\n/* Task.Delay(1) */"; await Task.Delay(123);', [2]],
        ['interpolated string', 'var value = $"https://example.test/{"/*"}"; await Task.Delay(123);', [1]],
        ['interpolated verbatim strings', 'var a = $@"https://example.test/{"//"}"; var b = @$"/* {"//"}"; await Task.Delay(123);', [1]],
        ['raw string', 'var value = """\nhttps://example.test/\n/* Task.Delay(1) */\n"""; await Task.Delay(123);', [4]],
        ['long raw delimiter', 'var value = """"https://example.test/ """ /* Task.Delay(1) */""""; await Task.Delay(123);', [1]],
        ['interpolated raw string', 'var value = $$"""https://example.test/{ {{"/*"}} Task.Delay(1)"""; await Task.Delay(123);', [1]],
        ['interpolation expression', 'var value = $"https://example.test/{await Task.Delay(123)}";', [1]],
        ['interpolation format', 'var value = $"{value:https://example.test/}"; await Task.Delay(123);', [1]],
        ['raw interpolation format', 'var value = $$"""{{value:https://example.test/}}"""; await Task.Delay(123);', [1]],
        ['nested interpolation', 'var value = $"https://example.test/{$"nested {"//"}"}"; await Task.Delay(123);', [1]],
        ['character literals', String.raw`var slash = '/'; var quote = '\"'; await Task.Delay(123);`, [1]],
        ['following lines', 'var value = "https://example.test/";\r\nawait Task.Delay(123);\r\nawait Task.Yield();', [2, 3]],
    ];
    for (const [name, text, lines] of cases) {
        assert.deepEqual(sitesIn(text).found, lines, name);
        plant('Source/Example.Specs/spec.cs', `${text}\nawait Task.Yield();`);
        const result = cli();
        assert.equal(result.status, 1, `${name}: ${result.stdout}${result.stderr}`);
        assert.match(result.stderr, /waits on the clock .*allows 1/, name);
    }
    assert.deepEqual(sitesIn('// Task.Delay(1);\n/* Task.Delay(2); */').found, []);
    assert.deepEqual(sitesIn('var value = "Task.Delay(1) // /*"; /* Task.Yield() */').found, []);
}

function testLauncher(plant, fixture) {
    const python = (process.env.PATH ?? '').split(delimiter).map(path => join(path, 'python3')).find(existsSync);
    if (!python) {
        console.log('Launcher self-test skipped: python3 is unavailable.');
        return;
    }
    const script = '.github/scripts/assert-timing-coupling-ratchet.mjs';
    const launcher = script.replace(/\.mjs$/, '.py');
    plant(launcher, readFileSync(fileURLToPath(new URL('./assert-timing-coupling-ratchet.py', import.meta.url)), 'utf8'));
    const launch = (args = [], env = process.env) => spawnSync(python, [join(fixture, launcher), fixture, ...args], { encoding: 'utf8', env });
    assert.equal(launch().status, 2, 'A launcher without its Node script cannot run.');
    plant(script, readFileSync(fileURLToPath(import.meta.url), 'utf8'));
    assert.equal(launch([], { ...process.env, PATH: fixture }).status, 2, 'A launcher without Node cannot run.');
    assert.equal(launch(['--report']).status, 0, 'The launcher must preserve success.');
    assert.equal(launch().status, 1, 'The launcher must preserve violations.');
    assert.equal(launch(['--unknown']).status, 2, 'The launcher must preserve cannot-run errors.');
}

function testCannotRun(plant, cli, fixture) {
    assert.equal(cli('--unknown').status, 2, 'Unknown options cannot run.');
    assert.equal(cli(join(fixture, 'nonexistent')).status, 2, 'Missing roots cannot run.');
    rmSync(join(fixture, baselinePath));
    assert.equal(cli().status, 2, 'Missing baselines cannot run.');
    mkdirSync(join(fixture, baselinePath));
    assert.equal(cli().status, 2, 'Unreadable baselines cannot run.');
    rmSync(join(fixture, baselinePath), { recursive: true });
    plant(baselinePath, 'malformed baseline');
    assert.equal(cli().status, 2, 'Malformed baselines cannot run.');
    plant(baselinePath, '');
    assert.equal(cli().status, 2, 'Empty baselines cannot run.');
    plant(baselinePath, '1 Source/Example.Specs/spec.cs\n');
    rmSync(join(fixture, 'Source'), { recursive: true });
    plant('Source', 'not a directory');
    assert.equal(cli().status, 2, 'Scan I/O failures cannot run.');
}

function selfTest(root) {
    const work = join(root, '.ai-work');
    mkdirSync(work, { recursive: true });
    const fixture = mkdtempSync(join(work, 'timing-ratchet-self-test-'));
    const plant = (path, text) => {
        mkdirSync(dirname(join(fixture, path)), { recursive: true });
        writeFileSync(join(fixture, path), text);
    };
    const cli = (...args) => spawnSync(process.execPath, [fileURLToPath(import.meta.url), ...(args.length ? args : [fixture])], { encoding: 'utf8' });
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
        testLexing(plant, cli);
        testLauncher(plant, fixture);
        testCannotRun(plant, cli, fixture);
        console.log('Self-test passed: planted violation rejected, adjacent explanations accepted, count ratchet retained.');
        console.log('Lexer regressions rejected; cannot-run errors returned exit 2.');
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
    process.exitCode = 2;
}
