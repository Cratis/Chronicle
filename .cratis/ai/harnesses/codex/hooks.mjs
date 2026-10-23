// cratis-ai-managed: harnesses/codex/hooks.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawn } from 'node:child_process';
import { statSync } from 'node:fs';
import { dirname, isAbsolute, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scripts = resolve(dirname(fileURLToPath(import.meta.url)), '../../hooks/scripts');
const guards = ['cratis-guard-store-mutations.sh', 'cratis-guard-secrets.sh', 'cratis-guard-pr-body.sh'];
const outputLimit = 64 * 1024;

function deny(reason) {
    return { code: 2, output: { hookSpecificOutput: {
        hookEventName: 'PreToolUse', permissionDecision: 'deny', permissionDecisionReason: reason,
    } }, reason };
}

// A detached POSIX child owns its process group. Clean that group even when the leader exits
// with descendants holding its pipes. No external timeout binary (macOS has none) is required.
function runGuard(script, input, cwd, executable, timeoutMs) {
    return new Promise(resolveResult => {
        let stdout = '', stderr = '', failure = '', closed = false, cleaned = false;
        let code = null, signal = null, stopping = false, escalation, reapDeadline;
        const child = spawn(executable, [script], { cwd, detached: true, stdio: ['pipe', 'pipe', 'pipe'] });
        const finish = () => {
            if (!closed || !cleaned) return;
            clearTimeout(deadline);
            clearTimeout(escalation);
            clearTimeout(reapDeadline);
            resolveResult({ code, signal, stdout, stderr, failure });
        };
        const killGroup = signalName => {
            if (!child.pid) return false;
            try { process.kill(-child.pid, signalName); return true; }
            catch (error) {
                if (error.code !== 'ESRCH') failure ||= 'Could not stop guard process group.';
                return false;
            }
        };
        const stop = () => {
            if (stopping) return;
            stopping = true;
            clearTimeout(deadline);
            if (!killGroup('SIGTERM')) { cleaned = true; finish(); return; }
            escalation = setTimeout(() => {
                killGroup('SIGKILL');
                cleaned = true;
                finish();
                // A failed reap is unknown, never permission to execute the pending tool.
                if (!closed) reapDeadline = setTimeout(() => {
                    failure ||= 'Guard did not close after termination.';
                    child.stdout.destroy(); child.stderr.destroy(); child.stdin.destroy();
                    child.unref(); closed = true; finish();
                }, 500);
            }, 500);
        };
        const deadline = setTimeout(() => { failure = 'Guard deadline exceeded.'; stop(); }, timeoutMs);
        child.on('error', () => { failure ||= 'Guard spawn failed.'; closed = true; stop(); finish(); });
        child.on('exit', (status, termination) => { code = status; signal = termination; stop(); });
        child.on('close', () => { closed = true; finish(); });
        const collect = (stream, append) => stream.on('data', chunk => {
            append(chunk.toString());
            if (Buffer.byteLength(stdout) + Buffer.byteLength(stderr) > outputLimit) {
                stdout = ''; stderr = ''; failure = 'Guard output exceeded limit.'; stop();
            }
        });
        collect(child.stdout, text => { stdout += text; });
        collect(child.stderr, text => { stderr += text; });
        child.stdin.on('error', error => {
            // An early guard verdict may deliberately close stdin without consuming it.
            if (error.code !== 'EPIPE') { failure ||= 'Guard input failed.'; stop(); }
        });
        child.stdin.end(input);
    });
}

/** Options are for isolated tests; the native entrypoint always uses the managed scripts and deadline. */
export async function shellGuard(input, { scriptsDirectory = scripts, executable = 'bash', timeoutMs = 10_000 } = {}) {
    try {
        const payload = JSON.parse(input);
        if (!payload || payload.hook_event_name !== 'PreToolUse' || payload.tool_name !== 'Bash' ||
            typeof payload.session_id !== 'string' || !payload.session_id.trim() ||
            typeof payload.cwd !== 'string' || !isAbsolute(payload.cwd) || !statSync(payload.cwd).isDirectory() ||
            typeof payload.tool_input?.command !== 'string' || !payload.tool_input.command.trim() ||
            payload.tool_input.command.includes('\0')) return deny('Malformed Codex shell-hook payload.');
        // Preflight BOTH guards: a missing later guard cannot be hidden by an earlier allowance.
        for (const guard of guards) {
            if (!statSync(join(scriptsDirectory, guard)).isFile()) return deny(`Missing managed guard: ${guard}.`);
        }
        const context = [];
        for (const guard of guards) {
            const result = await runGuard(join(scriptsDirectory, guard), JSON.stringify(payload), payload.cwd, executable, timeoutMs);
            if (result.failure || result.signal || result.code !== 0) {
                return deny(`${guard}: ${result.failure || (result.signal ? `terminated by ${result.signal}` : `exit ${result.code}`)}${result.stderr.trim() ? `\n${result.stderr.trim()}` : ''}`);
            }
            if (result.stderr.trim()) context.push(result.stderr.trim());
            if (result.stdout.trim()) {
                const output = JSON.parse(result.stdout);
                const specific = output.hookSpecificOutput;
                if (specific?.permissionDecision === 'deny' || output.decision === 'block') {
                    return deny(specific?.permissionDecisionReason || output.reason || `Blocked by ${guard}.`);
                }
                // Preserve warnings, not child allow/rewrite decisions or unsupported top-level fields.
                if (typeof output.systemMessage === 'string') context.push(output.systemMessage);
                if (typeof specific?.additionalContext === 'string') context.push(specific.additionalContext);
            }
        }
        return { code: 0, output: context.length ? { hookSpecificOutput: {
            hookEventName: 'PreToolUse', additionalContext: [...new Set(context)].join('\n'),
        } } : undefined };
    } catch {
        return deny('Codex shell guard could not validate its input or run the managed guards.');
    }
}

function readInput() {
    return new Promise((accept, reject) => {
        let input = '';
        const timer = setTimeout(() => { process.stdin.destroy(); reject(new Error('Input deadline exceeded.')); }, 2000);
        process.stdin.setEncoding('utf8');
        process.stdin.on('data', chunk => {
            input += chunk;
            if (Buffer.byteLength(input) > 1024 * 1024) {
                process.stdin.destroy(); clearTimeout(timer); reject(new Error('Input too large.'));
            }
        });
        process.stdin.on('end', () => { clearTimeout(timer); accept(input); });
        process.stdin.on('error', error => { clearTimeout(timer); reject(error); });
    });
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    const result = await readInput().then(input => shellGuard(input), () => deny('Malformed or incomplete Codex hook input.'));
    if (result.output) process.stdout.write(`${JSON.stringify(result.output)}\n`);
    if (result.reason) process.stderr.write(`${result.reason}\n`);
    process.exitCode = result.code;
}
