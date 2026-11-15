// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useState } from 'react';
import { GraphProjectionPreview, GraphViewClauseDeclaration, GraphViewDeclaration, PreviewGraphProjection } from 'Features/ProjectionEditor';
import { buildViews, clausesFor, defaultGraphViewSettings, rootFor, type GraphViewSettings } from './graphViews';

/**
 * Previews graph projections: runs the events of the event store through the projection in memory on the server and keeps
 * the graph it left behind, and what the view tab reads from it.
 * @param eventStore The event store the projection targets.
 * @param namespace The namespace the projection targets.
 * @returns The preview, whether one is being made, the settings of the view tab and the operations on them.
 */
export const useGraphPreview = (eventStore: string, namespace: string) => {
    const [command, setValues, clearValues] = PreviewGraphProjection.use();
    const [preview, setPreview] = useState<GraphProjectionPreview | null>(null);
    const [declaration, setDeclaration] = useState('');
    const [isPerforming, setIsPerforming] = useState(false);
    const [settings, setSettings] = useState<GraphViewSettings>(defaultGraphViewSettings);

    const execute = useCallback(async (
        text: string,
        views?: GraphViewDeclaration[],
        view?: string,
        clauses?: GraphViewClauseDeclaration[]): Promise<GraphProjectionPreview | null> => {
        clearValues();
        setValues({ eventStore, namespace, declaration: text, views, view, clauses, firstOnly: false });
        const result = await command.execute();
        return result.response ?? null;
    }, [command, eventStore, namespace, setValues, clearValues]);

    const read = useCallback(async (text: string, schemaOf: GraphProjectionPreview | null, next: GraphViewSettings) => {
        const graph = schemaOf?.graph;
        const root = graph ? rootFor(graph, next) : undefined;
        const built = graph && root ? buildViews(graph, root, next.depth) : undefined;
        if (!built) {
            return schemaOf;
        }

        return await execute(text, built.views, built.rootView, clausesFor(next)) ?? schemaOf;
    }, [execute]);

    /**
     * Runs the events through the projection, then reads the view of the settings from the graph it left.
     * @param text The graph declaration.
     * @returns The preview.
     */
    const run = useCallback(async (text: string): Promise<GraphProjectionPreview | null> => {
        setIsPerforming(true);
        try {
            setDeclaration(text);
            const graphOnly = await execute(text);
            const result = await read(text, graphOnly, settings);
            setPreview(result);
            return result;
        } finally {
            setIsPerforming(false);
        }
    }, [execute, read, settings]);

    /**
     * Reads a view of the graph the last preview left, with new settings.
     * @param next The settings of the view tab.
     */
    const readView = useCallback(async (next: GraphViewSettings) => {
        setSettings(next);
        if (!preview?.graph) return;

        setIsPerforming(true);
        try {
            setPreview(await read(declaration, preview, next));
        } finally {
            setIsPerforming(false);
        }
    }, [declaration, preview, read]);

    /** Forgets the preview, for when another declaration is opened. */
    const clear = useCallback(() => setPreview(null), []);

    return { preview, isPerforming, settings, setSettings, run, readView, clear };
};
