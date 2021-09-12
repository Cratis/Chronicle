// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DefaultButton, INavLinkGroup, INavStyles, Nav, PrimaryButton, Stack, StackItem } from '@fluentui/react';
import { BrowserRouter as Router, Route } from 'react-router-dom';

import './App.scss';

const navLinkGroups: INavLinkGroup[] = [
    {
        links: [

            {
                name: 'Event Log',
                icon: 'StackIndicator',
                url: '#',
                disabled: true,
                key: 'key2',
                target: '_blank',
            },
            {
                name: 'Streams',
                icon: 'Streaming',
                url: '#',
                disabled: true,
                key: 'key2',
                target: '_blank',
            },
            {
                name: 'Schemas',
                icon: 'AllApps',
                url: '#',
                key: 'key1',
                target: '_blank',
            },
            {
                name: 'Migrations',
                icon: 'History',
                url: '#',
                disabled: true,
                key: 'key2',
                target: '_blank',
            },
            {
                name: 'Projections',
                icon: 'GenericScanFilled',
                url: '#',
                key: 'key3',
                isExpanded: true,
                target: '_blank',
            }
        ]
    },
];

const navStyles: Partial<INavStyles> = {
    root: {
        width: 150,
        boxSizing: 'border-box'
    },
};

export const App = () => {
    return (
        <>
            <Router>
                <Stack horizontal>
                    <StackItem>

                        <Nav
                            selectedKey="key3"
                            ariaLabel="Nav basic example"
                            styles={navStyles}
                            groups={navLinkGroups}
                        />
                    </StackItem>
                    <StackItem>
                        <Route exact path="/">
                            <h1>Hello world</h1>

                            <DefaultButton>Cancel</DefaultButton>
                            <PrimaryButton>Apply</PrimaryButton>

                        </Route>

                    </StackItem>


                </Stack>
            </Router>
        </>
    )
};