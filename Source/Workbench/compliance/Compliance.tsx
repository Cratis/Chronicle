// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NavigationPage, NavigationItem } from '../Components/Navigation';
import * as icons from '@mui/icons-material';
import { DataSubjects } from './GDPR/DataSubjects';

const navigationItems: NavigationItem[] = [
    {
        title: 'GDPR',
        icon: <icons.HowToReg/>,
        targetPath: 'gdpr',
        children: [{
            title: 'Data Subjects',
            icon: <icons.Person/>,
            targetPath: 'data-subjects',
            content: <DataSubjects />
        }]
    }
];


export const Compliance = () => {
    return (
        <NavigationPage navigationItems={navigationItems}/>
    );
};
