// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import {
    registerProjectionLanguage,
    languageId,
    setReadModelSchema,
    setReadModelSchemas,
    setEventSchemas,
    setEventSequences,
    setCreateReadModelCallback,
    setEditReadModelCallback,
    setDraftReadModel,
} from '@cratis/screenplay-language/projection';
import type { ReadModelInfo } from '@cratis/screenplay-language/projection';

export * from './ProjectionEditor';

let disposer: { dispose(): void } | undefined;

export function registerProjectionDefinitionLanguage(monaco: typeof Monaco): void {
    disposer = registerProjectionLanguage(monaco);
}

export function disposeProjectionDefinitionLanguage(): void {
    disposer?.dispose();
    disposer = undefined;
}

export {
    languageId,
    setReadModelSchema,
    setReadModelSchemas,
    setEventSchemas,
    setEventSequences,
    setCreateReadModelCallback,
    setEditReadModelCallback,
    setDraftReadModel,
};
export type { ReadModelInfo };
export type { JsonSchema } from '@cratis/components/types';
