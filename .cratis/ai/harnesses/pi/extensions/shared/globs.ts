// cratis-ai-managed: harnesses/pi/extensions/shared/globs.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Globs that match every file. A rule or skill that declares one is not scoped to a path. */
export const universalGlobs = new Set(['**', '**/*', '*']);

function escapeRegExp(text: string): string {
    return text.replace(/[.+^$()|[\]\\]/g, '\\$&');
}

/** Converts the glob dialect used by `applyTo` and `paths` (`**`, `*`, `?`, `{a,b}`) into an anchored RegExp. */
export function globToRegExp(glob: string): RegExp {
    let pattern = '';
    for (let index = 0; index < glob.length; index++) {
        const character = glob[index];
        if (character === '*') {
            if (glob[index + 1] === '*') {
                if (glob[index + 2] === '/') {
                    pattern += '(?:.*/)?';
                    index += 2;
                } else {
                    pattern += '.*';
                    index += 1;
                }
            } else {
                pattern += '[^/]*';
            }
        } else if (character === '?') {
            pattern += '[^/]';
        } else if (character === '{') {
            const close = glob.indexOf('}', index);
            if (close > index) {
                pattern += `(?:${glob.slice(index + 1, close).split(',').map(part => escapeRegExp(part.trim())).join('|')})`;
                index = close;
            } else {
                pattern += '\\{';
            }
        } else {
            pattern += escapeRegExp(character);
        }
    }
    return new RegExp(`^${pattern}$`);
}

/**
 * Why a glob cannot scope a skill trigger, or undefined when it can. A trigger must be a non-empty,
 * repository-relative, balanced glob that does not match every file, otherwise it would either never fire
 * or fire on every write.
 */
export function globProblem(glob: string): string | undefined {
    if (glob.trim().length === 0) return 'is empty';
    if (glob !== glob.trim()) return 'has leading or trailing whitespace';
    if (glob.startsWith('/') || glob.includes('\\')) return 'must be a repository-relative glob using forward slashes';
    if (universalGlobs.has(glob)) return 'matches every file';
    if (glob.split('{').length !== glob.split('}').length) return 'has unbalanced braces';
    try {
        globToRegExp(glob);
    } catch {
        return 'does not compile to a matcher';
    }
    return undefined;
}
