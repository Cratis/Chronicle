// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { CratisComponentsProvider } from '@cratis/components/Common';
import ReactDOM from 'react-dom/client';
import 'primeicons/primeicons.css';
import './index.css';
import React from 'react';
import App from "./App";
import { configure as configureMobx } from 'mobx';
import { Bindings } from '../Bindings';
import { primeUiLicense } from './primeUiLicense';

Bindings.initialize();

configureMobx({
    enforceActions: 'never'
});

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <CratisComponentsProvider value={{ ripple: true, unstyled: true, license: primeUiLicense }}>
            <App />
        </CratisComponentsProvider>
    </React.StrictMode>
);
