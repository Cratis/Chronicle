// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Nav, INavLinkGroup, INavLink } from '@fluentui/react';
import { useState } from 'react';

import './App.scss';

const groups: INavLinkGroup[] = [
    {
        name: 'Events',
        links: [
            {
                name: 'Types',
                key: 'types',
                url: '#'
            },
            {
                name: 'Migrations',
                key: 'migrations',
                url: '#'
            }
        ]
    },
    {
        name: 'Explore',
        links: [
            {
                name: 'EventLog',
                key: 'event-log',
                url: '#'
            },
            {
                name: 'Streams',
                key: 'streams',
                url: '#'
            }
        ]
    }
];

export const App = () => {
    const [selectedNav, setSelectedNav] = useState('schemas');

    const navItemClicked = (ev?: React.MouseEvent<HTMLElement>, item?: INavLink) => {
        setSelectedNav(item?.key || '');
    };

    return (
        <div>
            <Nav groups={groups} onLinkClick={navItemClicked} selectedKey={selectedNav} />
        </div>
    );
};

