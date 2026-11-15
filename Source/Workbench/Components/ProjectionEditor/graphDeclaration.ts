// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';

/**
 * Matches the line that makes a declaration a graph projection: `projection <Name> => graph <GraphName>`.
 * The kernel decides the same way, so the editor and the server agree on what a graph declaration is.
 */
const graphProjectionPattern = /^[ \t]*projection[ \t]+[\w.]+[ \t]*=>[ \t]*graph[ \t]+[\w.]+/m;

/**
 * Gets whether a declaration holds a graph projection rather than a projection onto a read model.
 * @param declaration The declaration text.
 * @returns True when it declares `projection ... => graph ...`.
 */
export const isGraphDeclaration = (declaration: string): boolean => graphProjectionPattern.test(declaration);

/**
 * A graph projection to start from: people, groups, the MEMBER_OF edge between them and groups nested in groups.
 * It reacts to the events of the Organization example, so register event types of those names to try it, or
 * change the event names to events the event store holds.
 */
export const organizationGraphExample = `graph Organization
  node Person
    personId Uuid key
    name String
  node Group
    groupId Uuid key
    name String
  edge MEMBER_OF source Person target Group
    since DateTime
  edge PART_OF source Group target Group

projection OrganizationGraph => graph Organization
  node Person
    from PersonRegistered key personId
      name = name
    remove with PersonRemoved key personId detach
  node Group
    from GroupCreated key groupId
      name = name
  edge MEMBER_OF
    from MemberAdded source personId target groupId
      since = $eventContext.occurred
    remove with MemberRemoved source personId target groupId
  edge PART_OF
    from GroupNested source childId target parentId
`;

/**
 * The id of the editor language used for graph declarations.
 * The projection language does not know graph declarations, so it would mark every line of one as an error.
 */
export const graphProjectionLanguageId = 'chronicle-graph-projection';

const keywords = [
    'concept', 'graph', 'node', 'edge', 'source', 'target', 'identified', 'by', 'key', 'optional',
    'projection', 'from', 'remove', 'with', 'detach', 'increment', 'decrement', 'count', 'add', 'subtract',
    'every', 'for', 'each'
];

const typeKeywords = ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'];

/**
 * The tokenizer of graph declarations.
 */
export const graphProjectionLanguage: languages.IMonarchLanguage = {
    keywords,
    typeKeywords,
    tokenizer: {
        root: [
            [/#.*$/, 'comment'],
            [/\$[A-Za-z]\w*/, 'variable.predefined'],
            [/"[^"]*"/, 'string'],
            [/\d+(\.\d+)?/, 'number'],
            [/=>|\+=|-=|=/, 'operator'],
            [/[A-Za-z_]\w*/, { cases: { '@typeKeywords': 'type', '@keywords': 'keyword', '@default': 'identifier' } }]
        ]
    }
};

const registered = new WeakSet<object>();

/**
 * Registers the graph declaration language with an editor instance, once.
 * @param monaco The editor instance.
 */
export const registerGraphProjectionLanguage = (monaco: typeof import('monaco-editor')): void => {
    if (registered.has(monaco)) return;
    registered.add(monaco);
    monaco.languages.register({ id: graphProjectionLanguageId });
    monaco.languages.setLanguageConfiguration(graphProjectionLanguageId, { comments: { lineComment: '#' } });
    monaco.languages.setMonarchTokensProvider(graphProjectionLanguageId, graphProjectionLanguage);
};
