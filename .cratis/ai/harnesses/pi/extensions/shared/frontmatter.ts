// cratis-ai-managed: harnesses/pi/extensions/shared/frontmatter.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function unquote(value: string): string {
    return value.trim().replace(/^["']|["']$/g, '');
}

/** The text between the opening and closing `---` markers, or `undefined` when there is no terminated frontmatter. */
export function frontmatterText(content: string): string | undefined {
    if (!content.startsWith('---\n')) return undefined;
    const end = content.indexOf('\n---\n', 4);
    return end < 0 ? undefined : content.slice(4, end);
}

/**
 * Reads the YAML-ish frontmatter the corpus uses: scalar `key: value` lines and `key:` followed by
 * `  - item` lines. Anything richer is not used by the rules and skills and is deliberately not supported.
 */
export function frontmatter(content: string): Map<string, string[]> {
    const fields = new Map<string, string[]>();
    const text = frontmatterText(content);
    if (text === undefined) return fields;
    let current: string | undefined;
    for (const line of text.split('\n')) {
        const item = /^\s+-\s+(.*)$/.exec(line);
        if (item && current) {
            fields.get(current)!.push(unquote(item[1]));
            continue;
        }
        const scalar = /^([A-Za-z][\w-]*):\s*(.*)$/.exec(line);
        if (!scalar) continue;
        current = scalar[1];
        const value = unquote(scalar[2]);
        fields.set(current, value ? value.split(',').map(unquote).filter(Boolean) : []);
    }
    return fields;
}

/** One `key: value` line under a top-level block map such as `metadata`, as written, not interpreted. */
export interface FrontmatterEntry {
    /** The number of spaces the key is indented by, or -1 when the indentation contains a tab. */
    indent: number;
    /** The text after the colon, trimmed and otherwise untouched: quotes, a trailing comment and block scalar indicators included. */
    raw: string;
    /** True when more-indented lines follow the entry, so its value is a block scalar, a multi-line scalar or a nested map. */
    continues: boolean;
}

/** The result of reading the block map under a top-level key. */
export interface FrontmatterBlock {
    /** The text after `name:` on the key's own line, trimmed. Non-empty for an inline value such as a flow map. */
    inline: string;
    /** The entries the block holds, in order. A more-indented line belongs to the entry above it and is not an entry itself. */
    entries: Map<string, FrontmatterEntry>;
}

/**
 * Reads the block under a top-level `name:` key without interpreting the values. Blank and comment lines are skipped.
 * Returns `undefined` when the key is absent or the frontmatter is unterminated.
 */
export function frontmatterBlock(content: string, name: string): FrontmatterBlock | undefined {
    const text = frontmatterText(content);
    if (text === undefined) return undefined;
    let block: FrontmatterBlock | undefined;
    let inside = false;
    let last: FrontmatterEntry | undefined;
    let skipAbove: number | undefined;
    for (const line of text.split('\n')) {
        // Any line that starts in column 0 with something other than a comment ends the current block, whatever its
        // spelling (a quoted key, a space before the colon). Only a real `name:` key starts this block.
        if (/^[^\s#]/.test(line)) {
            const top = /^([A-Za-z][\w-]*):(.*)$/.exec(line);
            inside = top?.[1] === name;
            last = undefined;
            skipAbove = undefined;
            // A comment after the key (`metadata: # note`) is not a value, so the block below it is still the block form.
            if (inside) block ??= { inline: /^\s+#/.test(top![2]) ? '' : top![2].trim(), entries: new Map() };
            continue;
        }
        if (!inside || !block || line.trim() === '' || /^\s*#/.test(line)) continue;
        const indent = /^ *\t/.test(line) ? -1 : /^( *)/.exec(line)![1].length;
        // Lines under a sibling key the entry pattern does not read (a quoted key, `? complex`) are not entries.
        if (skipAbove !== undefined && indent > skipAbove) continue;
        skipAbove = undefined;
        const entry = /^\s+([A-Za-z][\w-]*):(.*)$/.exec(line);
        if (entry && (!last || indent <= last.indent)) {
            last = { indent, raw: entry[2].trim(), continues: false };
            block.entries.set(entry[1], last);
        } else if (last && (indent < 0 || indent > last.indent)) {
            last.continues = true;
        } else {
            // At the entry's indentation or less, so a sibling key ends the entry even when its spelling is not read here.
            last = undefined;
            skipAbove = indent;
        }
    }
    return block;
}

/**
 * The string a raw scalar spells when it is written as one complete double-quoted or single-quoted string on one
 * line, with nothing after the closing quote. Anything else, including a string with an escape or an embedded
 * quote, a trailing `# comment`, an unquoted value and a block scalar, yields `undefined`: it is not guessed at.
 */
export function quotedScalar(raw: string): string | undefined {
    return /^"([^"\\]*)"$/.exec(raw)?.[1] ?? /^'([^']*)'$/.exec(raw)?.[1];
}

/**
 * Reads the nested string map under a top-level `name:` key, as Agent Skills `metadata` uses. Only the one form the
 * corpus supports is read: an entry indented two spaces whose value is a complete one-line quoted string (see
 * `quotedScalar`). Values stay whole strings, without comma splitting. An entry in any other form is left out of the
 * map, so a caller never sees a guess such as `>-`; `frontmatterBlock` gives the raw entries for reporting them.
 * Returns `undefined` when the key is absent or the frontmatter is unterminated.
 */
export function frontmatterMap(content: string, name: string): Map<string, string> | undefined {
    const block = frontmatterBlock(content, name);
    if (!block) return undefined;
    const map = new Map<string, string>();
    // A value on the key's own line (a flow map, even one that continues below) is not the supported block form.
    if (block.inline !== '') return map;
    for (const [key, entry] of block.entries) {
        const value = entry.indent === 2 && !entry.continues ? quotedScalar(entry.raw) : undefined;
        if (value !== undefined) map.set(key, value);
    }
    return map;
}
