// cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/scripts/model-first-status.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { readFileSync, realpathSync, statSync } from 'node:fs';
import { dirname, isAbsolute, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

function git(repository, args, allowed = [0]) {
    const result = spawnSync('git', args, { cwd: repository, encoding: 'utf8', timeout: 10000, maxBuffer: 16 * 1024 * 1024 });
    if (result.error || !allowed.includes(result.status)) {
        throw new Error(`git ${args[0]}: ${result.error?.message ?? result.stderr.trim() ?? result.status}`);
    }
    return result;
}

const names = result => result.stdout.split('\0').filter(Boolean);
const play = path => path.toLowerCase().endsWith('.play');
const under = (path, root) => root === '.' || path.startsWith(`${root}/`);
const ignored = /(^|\/)(\.[^/]+|node_modules|bin|obj|build|artifacts|dist|out|packages|documentation|docs|samples?|examples?|scratch|[^/]+\.(?:Specs|Tests)|Specs|Tests|for_[^/]+|fixtures|Cases|test[-_]?assets)(\/|$)/i;
// The legacy model location is a project root, not arbitrary hidden-folder content.
const projectModel = path => !ignored.test(path.startsWith('.cratis/screenplay/') ? path.slice('.cratis/screenplay/'.length) : path);

const objectValue = (value, name) => {
    if (value === undefined || value === null) return {};
    if (typeof value !== 'object' || Array.isArray(value)) throw new Error(`${name} must be an object`);
    return value;
};

function projectPath(value, name) {
    if (typeof value !== 'string' || !value.trim()) throw new Error(`${name} must be a nonempty repository-relative path`);
    if (isAbsolute(value) || value.startsWith('~') || value.split(/[\\/]/).includes('..')) {
        throw new Error(`${name} must stay inside the repository`);
    }
    return value;
}

function optionalText(value, name) {
    if (value === undefined || value === null) return undefined;
    if (typeof value !== 'string' || !value.trim()) throw new Error(`${name} must be a nonempty string`);
    return value;
}

// The resolved project process block. Defaults reproduce the behavior of a project without one.
function resolveProcess(value) {
    const process = objectValue(value, 'process');
    const decisions = objectValue(process.decisions, 'process.decisions');
    const workItems = objectValue(process.workItems, 'process.workItems');
    const acceptance = objectValue(process.acceptance, 'process.acceptance');
    const agents = objectValue(process.agents, 'process.agents');
    const addenda = {};
    for (const [agent, files] of Object.entries(agents)) {
        if (!Array.isArray(files) || !files.every(file => typeof file === 'string')) {
            throw new Error(`process.agents.${agent} must be an array of repository-relative paths`);
        }
        addenda[agent] = files.map(file => projectPath(file, `process.agents.${agent}`));
    }
    return {
        decisions: {
            path: decisions.path === undefined || decisions.path === null ? 'decisions' : projectPath(decisions.path, 'process.decisions.path'),
            id: optionalText(decisions.id, 'process.decisions.id') ?? null,
        },
        workItems: {
            path: workItems.path === undefined || workItems.path === null ? null : projectPath(workItems.path, 'process.workItems.path'),
        },
        acceptance: {
            meaning: optionalText(acceptance.meaning, 'process.acceptance.meaning') ?? 'user',
            commit: optionalText(acceptance.commit, 'process.acceptance.commit') ?? 'user',
        },
        agents: addenda,
    };
}

function readConfiguration(repository) {
    let text;
    try {
        text = readFileSync(join(repository, '.cratis/ai.json'), 'utf8');
    } catch (error) {
        if (error.code === 'ENOENT') return { root: undefined, frozen: false, frozenReason: null, process: resolveProcess(undefined) };
        throw error;
    }
    const config = JSON.parse(text);
    if (!config || typeof config !== 'object' || Array.isArray(config)) throw new Error('.cratis/ai.json must be an object');
    const root = config.mcpServers?.screenplay?.root;
    if (root !== undefined && root !== null && (typeof root !== 'string' || !root.trim())) {
        throw new Error('mcpServers.screenplay.root must be a nonempty directory path');
    }
    const modelFirst = objectValue(config.modelFirst, 'modelFirst');
    if (modelFirst.frozen !== undefined && modelFirst.frozen !== null && typeof modelFirst.frozen !== 'boolean') {
        throw new Error('modelFirst.frozen must be a boolean');
    }
    const frozenReason = optionalText(modelFirst.reason, 'modelFirst.reason') ?? null;
    return {
        root: root ?? undefined,
        frozen: modelFirst.frozen === true,
        frozenReason: modelFirst.frozen === true ? frozenReason : null,
        process: resolveProcess(config.process),
    };
}

function normalizeRoot(repository, root) {
    if (typeof root !== 'string' || !root.trim()) throw new Error('--root requires a nonempty directory path');
    if (isAbsolute(root)) throw new Error('Model root must be repository-relative');
    const path = relative(repository, resolve(repository, root)).replaceAll('\\', '/');
    if (path === '..' || path.startsWith('../')) throw new Error('Model root must stay inside the repository');
    try {
        if (!statSync(join(repository, path)).isDirectory()) throw new Error(`Model root is not a directory: ${root}`);
    } catch (error) {
        if (error.code !== 'ENOENT') throw error;
    }
    return path || '.';
}

function locateRoot(repository, paths) {
    if (paths.some(path => path.startsWith('.cratis/screenplay/'))) return '.cratis/screenplay';
    const directories = paths.map(path => dirname(path));
    if (directories.length) {
        let common = directories[0].split('/');
        for (const directory of directories.slice(1)) {
            const parts = directory.split('/');
            const mismatch = common.findIndex((part, index) => part !== parts[index]);
            if (mismatch >= 0) common = common.slice(0, mismatch);
        }
        return common.join('/') || '.';
    }
    for (const folder of ['Source', 'src']) {
        try {
            if (statSync(join(repository, folder)).isDirectory()) return folder;
        } catch (error) {
            if (error.code !== 'ENOENT') throw error;
        }
    }
    return 'Screenplay';
}

/** Inspect HEAD and drafts without writing, staging or accepting a model. */
export function modelFirstStatus(cwd = process.cwd(), rootOverride) {
    const repository = git(cwd, ['rev-parse', '--show-toplevel']).stdout.trim();
    const { root: configured, frozen, frozenReason, process: projectProcess } = readConfiguration(repository);
    const head = git(repository, ['rev-parse', '--verify', '--quiet', 'HEAD'], [0, 1]);
    const committed = head.status === 0
        ? names(git(repository, ['ls-tree', '-r', '--name-only', '-z', 'HEAD'])).filter(play) : [];
    const indexed = names(git(repository, ['ls-files', '-z', '--cached'])).filter(play);
    const untracked = names(git(repository, ['ls-files', '-z', '--others', '--exclude-standard'])).filter(play);
    const paths = [...new Set([...committed, ...indexed, ...untracked])].sort();
    const projectCommitted = committed.filter(projectModel);
    const discoveryPaths = projectCommitted.length ? projectCommitted : paths.filter(projectModel);
    const root = normalizeRoot(repository, rootOverride ?? configured ?? locateRoot(repository, discoveryPaths));
    const inModel = path => under(path, root) && (rootOverride !== undefined || configured !== undefined || projectModel(path));
    const accepted = committed.filter(inModel).sort();
    const staged = indexed.filter(path => inModel(path) && !committed.includes(path)).sort();
    const drafts = { untracked: untracked.filter(inModel).sort(), staged };
    const modified = head.status === 0
        ? names(git(repository, ['diff', '--name-only', '-z', 'HEAD', '--'])).filter(path => accepted.includes(path)).sort() : [];
    const optedIn = !frozen && (configured !== undefined || accepted.length > 0);
    return {
        optedIn,
        reason: frozen ? 'frozen-model' : configured !== undefined ? 'configured-root' : accepted.length ? 'committed-model' : 'no-accepted-model',
        frozen, frozenReason, process: projectProcess,
        repository, modelRoot: root,
        rootSource: rootOverride !== undefined ? 'argument' : configured !== undefined ? 'configuration' : 'discovery',
        filesExamined: paths.length, modelFilesExamined: paths.filter(inModel).length,
        committed: accepted, drafts, modelChangesInProgress: modified,
        contract: accepted.length && !frozen ? 'HEAD' : null,
    };
}

function argumentsFor(args) {
    const options = {};
    for (let index = 0; index < args.length; index++) {
        if (args[index] === '--json') options.json = true;
        else if (args[index] === '--self-test') options.selfTest = true;
        else if (args[index] === '--root' && args[index + 1] && !args[index + 1].startsWith('--')) options.root = args[++index];
        else throw new Error('Usage: node model-first-status.mjs [--root <dir>] [--json] | --self-test');
    }
    return options;
}

async function main() {
    let options;
    try {
        options = argumentsFor(process.argv.slice(2));
        if (options.selfTest) {
            const { selfTest } = await import('./model-first-self-test.mjs');
            selfTest(modelFirstStatus);
            return;
        }
        const report = modelFirstStatus(process.cwd(), options.root);
        console.log(options.json ? JSON.stringify(report) : [
            `Model first: ${report.optedIn ? 'opted in' : 'not opted in'} (${report.reason}); root=${report.modelRoot}`,
            `Examined ${report.filesExamined} .play files; ${report.modelFilesExamined} under the root; ${report.committed.length} committed`,
            ...(report.frozen ? [`Frozen: ${report.frozenReason ?? 'no reason given'}; read-only unless the request names the model`] : []),
            `Drafts: ${report.drafts.untracked.length} untracked, ${report.drafts.staged.length} staged; ${report.modelChangesInProgress.length} model changes in progress; contract=${report.contract ?? 'none'}`,
        ].join('\n'));
        process.exitCode = report.optedIn ? 0 : 1;
    } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        if (options?.json || process.argv.includes('--json')) console.log(JSON.stringify({ error: message }));
        console.error(`model-first-status: could not run: ${message}`);
        process.exitCode = 2;
    }
}

if (process.argv[1] && realpathSync(process.argv[1]) === realpathSync(fileURLToPath(import.meta.url))) await main();
