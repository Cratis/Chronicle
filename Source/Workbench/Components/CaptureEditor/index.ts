// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Monaco from 'monaco-editor';
import { registerCaptureLanguage, languageId } from '@cratis/screenplay-language/capture';

export * from './CaptureEditor';

let disposer: { dispose(): void } | undefined;

export function registerCaptureDefinitionLanguage(monaco: typeof Monaco): void {
    disposer = registerCaptureLanguage(monaco);
}

export function disposeCaptureDefinitionLanguage(): void {
    disposer?.dispose();
    disposer = undefined;
}

export { languageId };
