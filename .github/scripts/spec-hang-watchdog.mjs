// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { appendFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { createInterface } from 'node:readline';

const quietMessages = new Set(['test-passed', 'test-not-run', 'test-case-starting', 'test-case-finished',
    'test-method-starting', 'test-method-finished', 'test-collection-starting', 'test-collection-finished']);
const annotation = text => text.replaceAll('%', '%25').replaceAll('\r', '%0D').replaceAll('\n', '%0A');
function seconds(value, fallback, name) {
    const result = value === undefined ? fallback : Number(value);
    if (!Number.isFinite(result) || result <= 0) throw new Error(`${name} must be a positive number`);
    return result * 1000;
}
function messageFrom(line) {
    try { return JSON.parse(line); } catch { return null; }
}

// xUnit v3 4.0.2: src/common/MessagePartials at
// https://github.com/xunit/xunit/tree/v3-4.0.2/src/common/MessagePartials
// $type identifies messages; finish messages carry unique IDs, not display names.
export class SpecHangWatchdog {
    classes = new Map();
    tests = new Map();

    constructor({ directory, env = process.env, now = () => performance.now(), report = console.log }) {
        this.now = now;
        this.report = report;
        this.warning = seconds(env.SPEC_CLASS_HANG_WARN_SECONDS, 120, 'SPEC_CLASS_HANG_WARN_SECONDS');
        this.timeout = seconds(env.SPEC_CLASS_HANG_TIMEOUT_SECONDS, 300, 'SPEC_CLASS_HANG_TIMEOUT_SECONDS');
        this.progressFile = path.join(directory, 'class-progress.log');
        this.summaryFile = path.join(directory, 'spec-hang-summary.txt');
        writeFileSync(this.progressFile, 'Spec class progress (elapsed wall-clock seconds)\n', { flag: 'wx' });
    }

    progress(text) {
        appendFileSync(this.progressFile, `${text}\n`);
        this.report(`[specs] ${text}`);
    }

    elapsed(started) { return ((this.now() - started) / 1000).toFixed(1); }

    consume(line) {
        const message = messageFrom(line);
        switch (message?.$type) {
            case 'test-class-starting':
                this.classes.set(message.TestClassUniqueID, { name: message.TestClassName, started: this.now() });
                this.progress(`class start ${message.TestClassName}`);
                break;
            case 'test-class-finished':
                this.finishClass(message.TestClassUniqueID);
                break;
            case 'test-starting':
                this.tests.set(message.TestUniqueID, { name: message.TestDisplayName, classId: message.TestClassUniqueID, started: this.now() });
                break;
            case 'test-finished':
                this.tests.delete(message.TestUniqueID);
                break;
            default:
                if (!quietMessages.has(message?.$type)) this.reportMessage(message, line);
        }
    }

    reportMessage(message, line) {
        const readable = ['diagnostic-message', 'internal-diagnostic-message', 'error', 'test-failed', 'test-output'];
        if (!readable.includes(message?.$type)) return this.report(line);
        const details = [message.Message, message.Messages, message.StackTraces, message.Output]
            .flat().filter(value => typeof value === 'string' && value.length);
        if (!details.length) return this.report(line);
        const name = this.tests.get(message.TestUniqueID)?.name;
        this.report(`[${message.$type}]${name ? ` ${name}` : ''}\n${details.join('\n')}`);
    }

    finishClass(id) {
        const active = this.classes.get(id);
        if (active) this.progress(`class finish ${active.name} elapsed=${this.elapsed(active.started)}s`);
        this.classes.delete(id);
        for (const [testId, test] of this.tests) if (test.classId === id) this.tests.delete(testId);
    }

    summary(reason) {
        const lines = [reason, 'Open spec classes:'];
        for (const active of this.classes.values()) lines.push(`  ${active.name} elapsed=${this.elapsed(active.started)}s`);
        lines.push('Open tests:');
        for (const active of this.tests.values()) lines.push(`  ${active.name} elapsed=${this.elapsed(active.started)}s`);
        const text = lines.join('\n');
        writeFileSync(this.summaryFile, `${text}\n`);
        this.report(`::error::${annotation(text)}`);
        return text;
    }

    check(terminate) {
        const active = [...this.classes.values()];
        for (const entry of active) {
            if (!entry.warned && this.now() - entry.started >= this.warning) {
                entry.warned = true;
                const warning = `Long-running spec class ${entry.name} elapsed=${this.elapsed(entry.started)}s`;
                appendFileSync(this.progressFile, `${warning}\n`);
                this.report(`::warning::${annotation(warning)}`);
            }
        }
        if (active.some(entry => this.now() - entry.started >= this.timeout)) {
            this.stop();
            this.summary(`Spec class hang timeout (${this.timeout / 1000}s); terminating process tree (exit 125)`);
            terminate(Object.assign(new Error(`Spec class hang timeout: see ${this.summaryFile}`), { exitCode: 125 }));
        }
    }

    watch(stream, terminate) {
        const guarded = action => {
            try { action(); } catch (error) { this.stop(); terminate(error); }
        };
        this.lines = createInterface({ input: stream, crlfDelay: Infinity });
        this.lines.on('line', line => guarded(() => this.consume(line)));
        this.timer = setInterval(() => guarded(() => this.check(terminate)), Math.min(1000, this.timeout / 10));
    }

    stop() {
        clearInterval(this.timer);
        this.lines?.close();
    }
}
