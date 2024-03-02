// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NavigationPage, NavigationItem } from '../Components/Navigation';
import * as icons from '@mui/icons-material';
import { AllMicroservices } from 'API/configuration/microservices/AllMicroservices';
import { ConnectedClients } from './ConnectedClients';

export const Clients = () => {
    const [microservices] = AllMicroservices.use();

    const navigationItems = microservices.data.map(microservice => {
        return {
            title: microservice.name,
            icon: <icons.Cabin />,
            targetPath: microservice.id,
            routePath: ':microserviceId',
            content: <ConnectedClients/>
        } as NavigationItem;
    });

    return (
        <NavigationPage navigationItems={navigationItems}/>
    );
};
