// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import 'primeicons/primeicons.css';
// Mirrors the production app's index.css ordering: the token layer first, then the
// component stylesheets that consume it, then the PrimeReact 10 palette the app's own
// CSS is written against. The look itself comes from styled mode on the provider below.
import '@cratis/components/tokens';
import '@cratis/components/styles';
import '@cratis/components/primereact-v10-palette';
import './preview.css';

import type { Preview, Decorator } from '@storybook/react';
import React from 'react';
import { CratisComponentsProvider } from '@cratis/components/Common';
import { styledMode } from '@cratis/components/styled';
import { Arc } from '@cratis/arc.react';
import { MVVM } from '@cratis/arc.react.mvvm';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';
import { primeUiLicense } from '../primeUiLicense';

// Dialogs, popovers and toasts portal to <body>, and the dark scheme is switched on
// `.cratis-dark` - so the classes go on the body, as the application's index.html has them,
// or exactly those portaled pieces render in the light scheme.
document.body.classList.add('cratis-theme', 'cratis-dark');

const withProviders: Decorator = (Story) => (
    <CratisComponentsProvider value={{ ripple: true, license: primeUiLicense, ...styledMode() }}>
        <div className='cratis-theme cratis-dark'>
            <Arc development={true}>
                <MVVM>
                    <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
                        <Story />
                    </DialogComponents>
                </MVVM>
            </Arc>
        </div>
    </CratisComponentsProvider>
);

const preview: Preview = {
    decorators: [withProviders],
    parameters: {
        actions: { argTypesRegex: '^on[A-Z].*' },
        controls: { expanded: true },
        backgrounds: {
            default: 'dark',
            values: [
                { name: 'dark', value: '#1e293b' },
                { name: 'light', value: '#ffffff' },
            ],
        },
    },
};

export default preview;
