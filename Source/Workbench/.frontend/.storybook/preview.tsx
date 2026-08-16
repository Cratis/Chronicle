// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import 'primeicons/primeicons.css';
// Mirrors the production app's index.css ordering: the token layer first, then the
// component stylesheets that consume it, then the Cratis baseline theme.
import '@cratis/components/tokens';
import '@cratis/components/styles';
import '@cratis/components/theme';
import './preview.css';

import type { Preview, Decorator } from '@storybook/react';
import React from 'react';
import { CratisComponentsProvider } from '@cratis/components/Common';
import { Arc } from '@cratis/arc.react';
import { MVVM } from '@cratis/arc.react.mvvm';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';
import { primeUiLicense } from '../primeUiLicense';

const withProviders: Decorator = (Story) => (
    <CratisComponentsProvider value={{ ripple: true, unstyled: true, license: primeUiLicense }}>
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
