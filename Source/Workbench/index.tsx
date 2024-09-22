// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { PrimeReactProvider } from 'primereact/api';
import ReactDOM from 'react-dom/client';
import 'primeicons/primeicons.css';
import './Styles/tailwind.css';
import './Styles/theme.css';
import React from 'react';
import App from "./App";
import { configure as configureMobx } from 'mobx';
import { Bindings } from './Bindings';

Bindings.initialize();

configureMobx({
    enforceActions: 'never'
});

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <PrimeReactProvider value={{ ripple: true }}>
            <App />
        </PrimeReactProvider>
    </React.StrictMode>
);
