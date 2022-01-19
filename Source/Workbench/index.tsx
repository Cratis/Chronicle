// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';

import './index.scss';
import './styles/theme';

import { App } from './App';
import { ModalProvider } from '@cratis/fluentui';

ReactDOM.render(
    <ModalProvider>
        <BrowserRouter>
            <App />
        </BrowserRouter>
    </ModalProvider>,
    document.getElementById('root')
);
