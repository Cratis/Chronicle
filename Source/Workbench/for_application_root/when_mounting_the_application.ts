// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { type ReactElement } from 'react';
import { CratisComponentsProvider, type CratisComponentsProviderProps } from '@cratis/components/Common';

const mocks = vi.hoisted(() => ({ render: vi.fn(), createRoot: vi.fn() }));
vi.mock('react-dom/client', () => ({ default: { createRoot: mocks.createRoot } }));
vi.mock('@cratis/components/Common', () => ({ CratisComponentsProvider: () => undefined }));
vi.mock('@primereact/core', () => ({ PrimeReactProvider: () => undefined }));
vi.mock('../.frontend/App', () => ({ default: () => undefined }));
vi.mock('../Bindings', () => ({ Bindings: { initialize: vi.fn() } }));
vi.mock('../.frontend/primeIslands/CratisPreset', () => ({
    CratisPreset: {}, cratisDarkModeSelector: '.cratis-dark', primeReactCssLayer: 'primereact', primeReactCssLayerOrder: 'primereact'
}));
vi.mock('../.frontend/primeIslands/primeReactStyles', () => ({ primeReactStyles: {} }));

describe('when mounting the Workbench application', () => {
    beforeAll(async () => {
        vi.stubGlobal('document', { getElementById: () => ({ id: 'root' }) });
        mocks.createRoot.mockReturnValue({ render: mocks.render });
        await import('../.frontend/index');
    });
    afterAll(() => {
        vi.unstubAllGlobals();
        for (const module of ['react-dom/client', '@cratis/components/Common', '@primereact/core', '../.frontend/App', '../Bindings',
            '../.frontend/primeIslands/CratisPreset', '../.frontend/primeIslands/primeReactStyles']) vi.doUnmock(module);
    });

    it('should enable the global toaster on the production Components provider', () => {
        mocks.render.mock.calls.should.have.lengthOf(1);
        const root = mocks.render.mock.calls[0][0] as ReactElement<{ children: ReactElement<CratisComponentsProviderProps> }>;
        const provider = root.props.children;
        provider.type.should.equal(CratisComponentsProvider);
        provider.props.toaster!.should.equal(true);
        provider.props.value!.locale!.should.equal('en-US');
    });
});
