// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type {
    GraphPreviewEdgeType,
    GraphPreviewSchema,
    GraphViewClauseDeclaration,
    GraphViewDeclaration,
    GraphViewMemberDeclaration
} from 'Features/ProjectionEditor';

/**
 * What the view tab reads: where to start, how many levels of relations to follow and an optional equality filter on the root.
 */
export interface GraphViewSettings {
    /** The label of the node the view is anchored at; the first label of the graph when not set. */
    root?: string;
    /** The number of levels of relations to follow from the root. */
    depth: number;
    /** The property of the root a filter compares, when filtering. */
    property?: string;
    /** The value the property must equal. */
    value: string;
}

/**
 * The views built for a root label, and what the filter can name.
 */
export interface BuiltViews {
    /** The views, the root view first. */
    views: GraphViewDeclaration[];
    /** The name of the root view. */
    rootView: string;
    /** The properties of the root node. */
    properties: string[];
    /** The property that holds the key of the root node. */
    keyProperty: string;
}

/** The levels of relations the view tab offers. */
export const graphViewDepths = [0, 1, 2, 3];

/** What the view tab reads before it is changed. */
export const defaultGraphViewSettings: GraphViewSettings = { depth: 1, value: '' };

const lowerCamel = (text: string): string => text.toLowerCase().replace(/_([a-z0-9])/g, (_, character: string) => character.toUpperCase());

const upperFirst = (text: string): string => text.charAt(0).toUpperCase() + text.slice(1);

const directionsOf = (edge: GraphPreviewEdgeType, label: string): ('outgoing' | 'incoming')[] => {
    if (edge.sourceLabel === label && edge.targetLabel === label) return ['outgoing', 'incoming'];
    if (edge.sourceLabel === label) return ['outgoing'];
    if (edge.targetLabel === label) return ['incoming'];
    return [];
};

const uniqueName = (candidate: string, taken: Set<string>): string => {
    let name = candidate;
    for (let suffix = 2; taken.has(name); suffix++) name = `${candidate}${suffix}`;
    taken.add(name);
    return name;
};

/**
 * Builds views over a graph that read every property of a node and follow every edge that leaves or enters it, a number of levels deep.
 *
 * Every nested view is anchored at the node at the far end of the edge and also reads the properties of the edge that was followed,
 * as members named `edge` and the property name. A relation always holds every related item, in the order the kernel gives.
 * @param schema The graph the preview declares.
 * @param root The label of the node to start at.
 * @param depth The number of levels of relations to follow; zero reads the node alone.
 * @returns The views, or undefined when the graph has no such node label.
 */
export const buildViews = (schema: GraphPreviewSchema, root: string, depth: number): BuiltViews | undefined => {
    const nodeTypes = new Map(schema.nodeTypes.map(node => [node.label, node]));
    const rootNode = nodeTypes.get(root);
    if (!rootNode) return undefined;

    const views: GraphViewDeclaration[] = [];

    const build = (label: string, name: string, level: number, via?: GraphPreviewEdgeType): string => {
        const node = nodeTypes.get(label)!;
        const taken = new Set<string>(node.properties);
        const members: GraphViewMemberDeclaration[] = node.properties.map(property => ({ name: property, kind: 'property', source: property }));

        for (const property of via?.properties ?? []) {
            members.push({ name: uniqueName(`edge${upperFirst(property)}`, taken), kind: 'edgeProperty', source: property });
        }

        if (level < depth) {
            for (const edge of schema.edgeTypes) {
                for (const direction of directionsOf(edge, label)) {
                    const farLabel = direction === 'outgoing' ? edge.targetLabel : edge.sourceLabel;
                    const base = lowerCamel(edge.type);
                    const member = uniqueName(direction === 'outgoing' ? base : `incoming${upperFirst(base)}`, taken);
                    const view = build(farLabel, `${name}.${member}`, level + 1, edge);
                    members.push({ name: member, kind: 'relation', edge: edge.type, direction, cardinality: 'many', view });
                }
            }
        }

        views.push({ name, root: label, members });
        return name;
    };

    const rootView = build(root, root, 0);
    views.sort((left, right) => Number(right.name === rootView) - Number(left.name === rootView));
    return { views, rootView, properties: rootNode.properties, keyProperty: rootNode.keyProperty };
};

/**
 * Gets the clauses that select the roots of a view for the filter of the settings.
 * @param settings The settings of the view tab.
 * @returns A clause when a property and a value are given, none otherwise.
 */
export const clausesFor = (settings: GraphViewSettings): GraphViewClauseDeclaration[] =>
    settings.property && settings.value.trim().length > 0 ? [{ member: settings.property, value: settings.value.trim() }] : [];

/**
 * Gets the root label the view tab reads from: the one chosen when the graph still has it, the first of the graph otherwise.
 * @param schema The graph the preview declares.
 * @param settings The settings of the view tab.
 * @returns The label, or undefined when the graph has no nodes.
 */
export const rootFor = (schema: GraphPreviewSchema, settings: GraphViewSettings): string | undefined =>
    schema.nodeTypes.some(node => node.label === settings.root) ? settings.root : schema.nodeTypes[0]?.label;

/**
 * Gives every row of a view the `_id` the read model table keys its rows by.
 * @param rows The rows the preview read.
 * @param keyProperty The property that holds the key of the root node.
 * @returns The rows, each with an `_id`.
 */
export const withRowIds = (rows: Record<string, unknown>[], keyProperty: string): Record<string, unknown>[] =>
    rows.map((row, index) => ({ _id: String(row[keyProperty] ?? index), ...row }));
