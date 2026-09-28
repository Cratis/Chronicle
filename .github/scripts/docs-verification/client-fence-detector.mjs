// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fromMarkdown } from 'mdast-util-from-markdown';
import { gfm } from 'micromark-extension-gfm';
import { gfmFromMarkdown } from 'mdast-util-gfm';
import { mdxjs } from 'micromark-extension-mdxjs';
import { mdxFromMarkdown } from 'mdast-util-mdx';
import { visit } from 'unist-util-visit';

export function createClientFenceDetector(clientFenceLanguages) {
    return (content, isMdx = false) => {
        // The site handles YAML frontmatter and converts DocFX xrefs before MDX parsing.
        // Retain newlines so code node positions still refer to the original page.
        const siteContent = content
            .replace(/^---[ \t]*\r?\n[\s\S]*?\r?\n---[ \t]*(?=\r?\n|$)/, match => match.replace(/[^\r\n]/g, ' '))
            .replace(/<xref:([^>\n]+)>/g, (_, inner) => `\`${inner.split('?')[0]}\``);
        const tree = fromMarkdown(siteContent, {
            extensions: [gfm(), ...(isMdx ? [mdxjs()] : [])],
            mdastExtensions: [gfmFromMarkdown(), ...(isMdx ? [mdxFromMarkdown()] : [])]
        });
        const violations = [];
        visit(tree, 'code', node => {
            if (node.lang && clientFenceLanguages.has(node.lang.toLowerCase())) {
                violations.push(node.position.start.line);
            }
        });
        return violations;
    };
}
