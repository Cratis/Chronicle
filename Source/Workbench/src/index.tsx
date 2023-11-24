// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PrimeReactProvider } from 'primereact/api';
import ReactDOM from 'react-dom/client';
import 'primeicons/primeicons.css';
import './Styles/tailwind.css';
import './Styles/theme.css';
import 'reflect-metadata';
import React from 'react';
import App from "./App";


import { Module } from './MVVM';

Module.initialize();

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <PrimeReactProvider value={{ ripple: true }}>
            <App/>
        </PrimeReactProvider>
    </React.StrictMode>
);
