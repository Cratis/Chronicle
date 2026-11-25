// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeKind } from './ChangeKind';
import { ChangeRow } from './ChangeRow';
import { ComparisonBudget } from './ComparisonBudget';
import { LimitReason } from './LimitReason';
import { describeValue } from './describeValue';
import { hasOwn } from './hasOwn';

const kindOf = (value: unknown): string => value === null ? 'null' : Array.isArray(value) ? 'array' : typeof value;

const token = (name: string) => name.replace(/~/g, '~0').replace(/\//g, '~1');

/**
 * Compares two parsed JSON values within the comparison budgets. Arrays compare by index and
 * object members by name; there is no entity matching. Hitting the node or row budget stops the
 * walk, and any budget hit is recorded so the result is never presented as complete.
 */
export class ValueDiffer {
    readonly rows: ChangeRow[] = [];
    limit?: LimitReason;
    private visited = 0;
    private halted = false;

    /**
     * Walks both values, collecting differences.
     * @param left The left value.
     * @param right The right value.
     * @param path The JSON Pointer of the values.
     */
    compare(left: unknown, right: unknown, path = ''): void {
        if (!this.visit(2)) return;
        const kind = kindOf(left);
        if (kind !== kindOf(right)) this.add(path, 'changed', left, right);
        else if (kind === 'array') this.compareArrays(left as unknown[], right as unknown[], path);
        else if (kind === 'object') this.compareObjects(left as object, right as object, path);
        else if (left !== right) this.add(path, 'changed', left, right);
    }

    private compareArrays(left: unknown[], right: unknown[], path: string): void {
        const length = Math.max(left.length, right.length);
        for (let index = 0; index < length && !this.halted; index++) {
            const child = `${path}/${index}`;
            if (index >= left.length) this.addOne(child, 'added', undefined, right[index]);
            else if (index >= right.length) this.addOne(child, 'removed', left[index], undefined);
            else this.compare(left[index], right[index], child);
        }
    }

    private compareObjects(left: object, right: object, path: string): void {
        const leftNames = this.names(left);
        const rightNames = this.halted ? [] : this.names(right);
        if (this.halted) return;
        for (const name of [...new Set([...leftNames, ...rightNames])].sort()) {
            if (this.halted) return;
            const child = `${path}/${token(name)}`;
            const before = hasOwn(left, name) ? (left as Record<string, unknown>)[name] : undefined;
            const after = hasOwn(right, name) ? (right as Record<string, unknown>)[name] : undefined;
            if (!hasOwn(left, name)) this.addOne(child, 'added', undefined, after);
            else if (!hasOwn(right, name)) this.addOne(child, 'removed', before, undefined);
            else this.compare(before, after, child);
        }
    }

    private names(value: object): string[] {
        const remaining = ComparisonBudget.maxVisited - this.visited;
        const names: string[] = [];
        for (const name in value) {
            if (!hasOwn(value, name)) continue;
            if (names.length >= remaining) {
                this.halt('nodes');
                break;
            }
            names.push(name);
        }
        return names;
    }

    private addOne(path: string, kind: ChangeKind, before: unknown, after: unknown): void {
        if (this.visit(1)) this.add(path, kind, before, after, kind === 'added' ? 'after' : 'before');
    }

    private visit(count: number): boolean {
        this.visited += count;
        if (this.visited > ComparisonBudget.maxVisited) this.halt('nodes');
        return !this.halted;
    }

    private halt(reason: LimitReason): void {
        this.halted = true;
        this.limit ??= reason;
    }

    private add(path: string, kind: ChangeKind, before: unknown, after: unknown, only?: 'before' | 'after'): void {
        if (this.rows.length >= ComparisonBudget.maxRows) {
            this.halt('rows');
            return;
        }
        const row: ChangeRow = { path: this.shortPath(path), kind };
        if (only !== 'after') row.before = this.show(before);
        if (only !== 'before') row.after = this.show(after);
        this.rows.push(row);
    }

    private shortPath(path: string): string {
        if (path.length <= ComparisonBudget.maxPath) return path;
        this.limit ??= 'path';
        return `${path.slice(0, ComparisonBudget.maxPath)}…`;
    }

    private show(value: unknown): string {
        const description = describeValue(value);
        if (description.truncated) this.limit ??= 'preview';
        return description.text;
    }
}
