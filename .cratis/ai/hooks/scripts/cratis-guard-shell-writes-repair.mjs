// cratis-ai-managed: hooks/scripts/cratis-guard-shell-writes-repair.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Non-applicable commands fall back to reference checks; recognized invalid repairs fail closed.
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { existsSync, lstatSync, readFileSync, readlinkSync, realpathSync, statSync } from 'node:fs';
import { basename, dirname, isAbsolute, relative, resolve, sep } from 'node:path';

const managedFolders = ['.pi', '.claude', '.cursor', '.opencode', '.github'];

function literalWords(command) {
    // No shell operators, expansions, escapes, comments, globs or multiline constructs, even in quotes.
    if (/[\x00-\x1f\x7f$`\\;|&<>(){}*?!#~\[\]]/.test(command)) return [];
    // Require whitespace between whole words: do not interpret concatenated quote fragments.
    if (!/^ *(?:(?:'[^']*'|"[^"]*"|[^\s'"]+)(?: +|$))*$/.test(command)) return [];
    return command.match(/'[^']*'|"[^"]*"|[^\s'"]+/g) ?? [];
}

function unquote(word) {
    return /^["']/.test(word) ? word.slice(1, -1) : word;
}

function inside(path, directory) {
    const suffix = relative(directory, path);
    if (isAbsolute(suffix)) return false;
    return !['', '..'].includes(suffix.split(sep)[0]);
}

function addressedTarget(link, target) {
    return isAbsolute(target) ? target : `${dirname(link)}/${target}`;
}

function physicalPath(path, depth = 0) {
    if (depth >= 40) throw new Error('Symlink resolution limit');
    // Follow each symlink before '..', even when its destination does not exist yet.
    let physical = sep;
    for (const component of path.split(sep).filter(Boolean)) {
        const candidate = resolve(physical, component);
        physical = lstatIfPresent(candidate)?.isSymbolicLink()
            ? physicalPath(addressedTarget(candidate, readlinkSync(candidate)), depth + 1)
            : candidate;
    }
    return physical;
}

function lstatIfPresent(path) {
    try { return lstatSync(path); }
    catch (error) {
        if (error.code === 'ENOENT') return undefined;
        throw error;
    }
}

function controlPatterns() {
    // Read the write guard's literal policy, never execute/source it or maintain a second copy.
    const source = readFileSync(new URL('./cratis-guard-writes.sh', import.meta.url), 'utf8');
    const definition = source.match(/^control_patterns=\((.*)\)$/m)?.[1] ?? '';
    const words = definition.match(/'[a-z0-9_./*-]+'/g) ?? [];
    if (!words.length || words.join(' ') !== definition) throw new Error('Unsupported control policy');
    return words.map(unquote);
}

function matchesControl(name, pattern) {
    const expression = pattern.split('*').map(part => part.replaceAll('.', '\\.')).join('.*');
    return new RegExp(`^${expression}(?:/.*)?$`).test(name) || pattern.startsWith(`${name}/`);
}

function controlLink(root, link) {
    const names = [link, physicalPath(link)].map(path => relative(root, path).toLowerCase());
    const patterns = controlPatterns();
    // Repairs are also forbidden in executable/agent-definition trees, even if a manifest lists them.
    const executableTree = /(?:^|\/)(?:hooks|agents|guards|extensions|workflows)(?:\/|$)/;
    return names.some(name => executableTree.test(name) || patterns.some(pattern => matchesControl(name, pattern)));
}

function managedEntry(root, link) {
    const manifest = resolve(root, '.cratis/ai.manifest.json');
    if (!existsSync(manifest)) return false;
    const { Integrations } = JSON.parse(readFileSync(manifest, 'utf8'));
    return Array.isArray(Integrations) && Integrations.some(entry =>
        entry?.Path === relative(root, link) && typeof entry.Target === 'string');
}

function linkOperands(words) {
    if (words.length !== 4) return undefined;
    if (!/^ln -(?:s|sf|sfn)$/.test(words.slice(0, 2).join(' '))) return undefined;
    const [target, name] = words.slice(2).map(unquote);
    if ([target, name].some(operand => !operand || operand.startsWith('-'))) return undefined;
    if (unsafeLinkName(name)) return undefined;
    return { target, name, flag: words[1] };
}

function unsafeLinkName(name) {
    // A trailing slash or '/.' forces directory traversal even with ln -n.
    return name.endsWith('/') || basename(name) === '.' || name.split('/').includes('..');
}

function parentInFolder(root, link, folder) {
    const parent = physicalPath(dirname(link));
    const managedFolder = resolve(realpathSync(root), folder);
    return parent === managedFolder || inside(parent, managedFolder);
}

function atOrInside(path, directory) {
    return path === directory || inside(path, directory);
}

function corpusRepair(root, link, target) {
    const addressed = addressedTarget(link, target);
    const corpus = resolve(root, '.cratis/ai');
    return atOrInside(resolve(addressed), corpus) || atOrInside(physicalPath(addressed), corpus);
}

function corpusTarget(root, link, target) {
    const addressed = addressedTarget(link, target);
    if (!inside(resolve(addressed), resolve(root, '.cratis/ai'))) return false;
    return inside(physicalPath(addressed), resolve(root, '.cratis/ai'));
}

function replaceableDestination(link, flag) {
    const existing = lstatIfPresent(link);
    if (existing?.isDirectory()) return false;
    if (!existing?.isSymbolicLink()) return true;
    if (!existsSync(link)) return true;
    // Without -n, ln follows a destination symlink to a directory and writes inside its target.
    return !statSync(link).isDirectory() || flag === '-sfn';
}

function repositoryRoot(cwd) {
    try {
        const repository = execFileSync('git', ['-C', cwd, 'rev-parse', '--show-toplevel'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim();
        return realpathSync(repository);
    } catch { return undefined; }
}

function validateRepair(root, link, folder, { target, flag }) {
    try {
        assert(!controlLink(root, link), 'Agent control link');
        assert(managedEntry(root, link), 'Unmanaged link');
        assert(parentInFolder(root, link, folder), 'Redirected link parent');
        assert(corpusTarget(root, link, target), 'Non-corpus target');
        assert(replaceableDestination(link, flag), 'Directory destination');
    } catch {
        // Once identified as a corpus repair, unreadable/invalid policy or paths must not permit fallback.
        assert.fail('Corpus repair validation failed');
    }
}

function repairLink(cwd, words) {
    const operands = linkOperands(words);
    if (!operands) return false;
    const root = repositoryRoot(cwd);
    if (!root) return false;
    // Git reports the physical root on macOS; align the cwd alias without resolving the link itself.
    const link = resolve(realpathSync(cwd), operands.name);
    const folder = managedFolders.find(folder => inside(link, resolve(root, folder)));
    if (!folder) return false;
    if (!corpusRepair(root, link, operands.target)) return false;
    validateRepair(root, link, folder, operands);
    return true;
}

function admitted(cwd, command) {
    const words = literalWords(command);
    if (['cratis ai init', 'cratis ai update', 'cratis ai restore'].includes(words.slice(0, 3).join(' '))) {
        assert(words.slice(3).every(word => ['--force', '--dry-run'].includes(word)), 'Unsupported installer arguments');
        return true;
    }
    return repairLink(cwd, words);
}

try { process.exitCode = admitted(process.argv[2], process.argv[3]) ? 0 : 1; }
catch (error) { process.exitCode = error instanceof assert.AssertionError ? 2 : 1; }
