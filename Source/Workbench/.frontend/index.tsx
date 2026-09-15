// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { CratisComponentsProvider } from '@cratis/components/Common';
import { PrimeReactProvider } from '@primereact/core';
import ReactDOM from 'react-dom/client';
import 'primeicons/primeicons.css';
import './index.css';
import React from 'react';
import App from "./App";
import { configure as configureMobx } from 'mobx';
import { Bindings } from '../Bindings';
import { primeUiLicense } from './primeUiLicense';
import { CratisPreset, cratisDarkModeSelector, primeReactCssLayer, primeReactCssLayerOrder } from './primeIslands/CratisPreset';
import { primeReactStyles } from './primeIslands/primeReactStyles';

Bindings.initialize();

configureMobx({
    enforceActions: 'never'
});

// Components 4 owns its own markup and styling and no longer configures a renderer, so the two
// providers are mounted independently: `CratisComponentsProvider` for everything Components
// renders, and `PrimeReactProvider` for the PrimeReact primitives the Workbench still renders
// itself (tabs, popover, toggle buttons, knob, input tags, icon field, toolbar, and the
// grouping / server-sorted tables). PrimeReact 11 ships no CSS of its own, so those islands
// need the preset and the per-component `defaults` that used to arrive through `styledMode()`.
ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <CratisComponentsProvider value={{ locale: 'en-US' }}>
            <PrimeReactProvider
                license={primeUiLicense}
                ripple
                theme={{
                    preset: CratisPreset,
                    options: {
                        darkModeSelector: cratisDarkModeSelector,
                        cssLayer: { name: primeReactCssLayer, order: primeReactCssLayerOrder }
                    }
                }}
                defaults={primeReactStyles}>
                <App />
            </PrimeReactProvider>
        </CratisComponentsProvider>
    </React.StrictMode>
);
