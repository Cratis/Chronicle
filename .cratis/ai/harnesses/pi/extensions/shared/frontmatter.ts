// cratis-ai-managed: harnesses/pi/extensions/shared/frontmatter.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function unquote(value: string): string {
    return value.trim().replace(/^["']|["']$/g, '');
}

/**
 * Reads the YAML-ish frontmatter the corpus uses: scalar `key: value` lines and `key:` followed by
 * `  - item` lines. Anything richer is not used by the rules and skills and is deliberately not supported.
 */
export function frontmatter(content: string): Map<string, string[]> {
    const fields = new Map<string, string[]>();
    if (!content.startsWith('---\n')) return fields;
    const end = content.indexOf('\n---\n', 4);
    if (end < 0) return fields;
    let current: string | undefined;
    for (const line of content.slice(4, end).split('\n')) {
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
