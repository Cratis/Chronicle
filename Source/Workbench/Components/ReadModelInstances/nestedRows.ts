// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Json } from 'Features';

type Row = { [key: string]: Json };

/** A row of a nested collection, as shown in the table. Entries that are not objects are shown under a `value` field. */
export type NestedRow = Row & {
    /** Unique among the rows of the collection, whatever the entries carry as identifiers. */
    __nestedKey: string;
    /** The document the row was found in. */
    __sourceInstance: Json;
    /** The position within the array the row came from, when it came from one. */
    __arrayIndex?: number;
};

/** One page of a nested collection. */
export interface NestedRows {
    /** The number of rows in the whole collection. */
    total: number;
    /** The rows of the requested page; never more than the page size. */
    rows: NestedRow[];
    /** The fields the table has columns for, taken from the first row of the whole collection. */
    columns: string[];
}

const isObject = (value: unknown): value is Row => value !== null && typeof value === 'object' && !Array.isArray(value);

const fieldsOf = (entry: Json | undefined): Row => isObject(entry) ? entry : { value: entry ?? null };

/**
 * Pages through the rows found by following a path through documents. Rows are counted without being
 * built; only the rows of the requested window are materialized.
 */
class NestedWindow {
    total = 0;
    readonly rows: NestedRow[] = [];
    columns: string[] | undefined;

    constructor(private readonly _offset: number, private readonly _limit: number) {
    }

    /**
     * Takes in the value a path ended at: an array contributes a row per entry, an object contributes itself.
     * @param value The value at the end of the path.
     * @param source The document it was found in.
     */
    collect(value: Json | undefined, source: Json): void {
        if (Array.isArray(value)) {
            this.#collectEntries(value, source);
        } else if (isObject(value)) {
            this.#collectRow(value, source, undefined);
        }
    }

    #collectEntries(entries: Json[], source: Json): void {
        const first = this.total;
        const start = Math.max(0, this._offset - first);
        const end = Math.min(entries.length, Math.max(0, this._offset + this._limit - first));
        if (first === 0 && entries.length > 0) {
            this.#noteColumns(entries[0]);
        }
        for (let index = start; index < end; index++) {
            this.#addRow(first + index, entries[index], source, index);
        }
        this.total += entries.length;
    }

    #collectRow(value: Row, source: Json, arrayIndex: number | undefined): void {
        if (this.total === 0) {
            this.#noteColumns(value);
        }
        if (this.total >= this._offset && this.total < this._offset + this._limit) {
            this.#addRow(this.total, value, source, arrayIndex);
        }
        this.total++;
    }

    #noteColumns(entry: Json | undefined): void {
        this.columns = Object.keys(fieldsOf(entry)).filter(key => !key.startsWith('__'));
    }

    #addRow(ordinal: number, entry: Json | undefined, source: Json, arrayIndex: number | undefined): void {
        const row = { ...fieldsOf(entry), __nestedKey: `nested-${ordinal}`, __sourceInstance: source } as NestedRow;
        if (arrayIndex !== undefined) {
            row.__arrayIndex = arrayIndex;
        }
        this.rows.push(row);
    }
}

const walk = (value: Json | undefined, path: readonly string[], depth: number, visit: (end: Json | undefined) => void): void => {
    if (depth === path.length) {
        visit(value);
    } else if (Array.isArray(value)) {
        value.forEach(element => walk(element, path, depth, visit));
    } else if (isObject(value)) {
        walk(value[path[depth]], path, depth + 1, visit);
    }
};

/**
 * Finds the rows at the end of a path through the documents, in document and array order. The path may
 * pass through arrays; every element of such an array is followed.
 * @param documents The documents to look in.
 * @param path The path to follow in every document.
 * @param offset The number of rows to skip.
 * @param limit The most rows to build.
 * @returns The rows of the page, the number of rows in total and the columns to show.
 */
export const getNestedRows = (documents: readonly Json[], path: readonly string[], offset: number, limit: number): NestedRows => {
    const window = new NestedWindow(offset, limit);
    documents.forEach(document => walk(document, path, 0, end => window.collect(end, document)));
    return { total: window.total, rows: window.rows, columns: window.columns ?? [] };
};

const sameDocument = (a: Json, b: Json): boolean => {
    try {
        return JSON.stringify(a) === JSON.stringify(b);
    } catch {
        return false;
    }
};

/**
 * Decides what the selection becomes when a row is activated.
 * @param isNested Whether a nested collection is being browsed.
 * @param clicked The row that was activated.
 * @param selected The document that is selected now.
 * @returns The document to select, or null to clear the selection. Only a root row clears it, by being activated again.
 */
export const resolveSelection = (isNested: boolean, clicked: Row | null, selected: Json | null): Json | null => {
    if (isNested) return (clicked as NestedRow | null)?.__sourceInstance ?? null;
    return clicked && selected && sameDocument(selected, clicked) ? null : clicked;
};
