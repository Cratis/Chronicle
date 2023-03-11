// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllMicroservices } from 'API/configuration/microservices/AllMicroservices';
import { NavigationPage } from '../Components/Navigation/NavigationPage';
import * as icons from '@mui/icons-material';
import { NavigationItem, NavigationButton } from '../Components/Navigation';
import { EventTypes } from './EventTypes';

export const EventStore = () => {
    const [microservices] = AllMicroservices.use();

    const navigationItems = microservices.data.map(microservice => {
        return {
            title: microservice.name,
            icon: <icons.Cabin />,
            targetPath: microservice.id,
            children: [{
                title: 'Types',
                icon: <icons.DataObject />,
                targetPath: 'types',
                content: <EventTypes />
            }]
        } as NavigationItem;
    });

    return (
        <NavigationPage navigationItems={navigationItems}/>
    );
};
