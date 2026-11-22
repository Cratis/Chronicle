// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, type ReactNode } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { vi } from 'vitest';

// The external Page/DataPage render no DOM in these specs. Keep React's real renderer,
// effects and unmount cleanup without introducing a browser environment dependency.
export class an_empty_root {
    constructor() {
        const document = { nodeType: 9, addEventListener: vi.fn(), activeElement: undefined, defaultView: {} };
        const window = { document, HTMLIFrameElement: class {}, event: undefined };
        document.defaultView = window;
        vi.stubGlobal('window', window);
        vi.stubGlobal('document', document);
        vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true);
        this.root = createRoot({
            nodeType: 1, nodeName: 'DIV', tagName: 'DIV', namespaceURI: 'http://www.w3.org/1999/xhtml',
            ownerDocument: document, addEventListener: vi.fn(), removeEventListener: vi.fn(), textContent: ''
        } as unknown as HTMLElement);
    }

    private readonly root: Root;

    async render(children: ReactNode) {
        await act(async () => this.root.render(children));
    }

    async unmount() {
        await act(async () => this.root.unmount());
    }
}
