// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CardFooter } from 'Components/Common/CardFooter';
import { StoreCard } from 'Components/Common/StoreCard';
import { HomeViewModel } from './HomeViewModel';
import { withViewModel } from '@cratis/applications.react.mvvm';
import { useRelativePath } from '../Utils/useRelativePath';

export const Home = withViewModel(HomeViewModel, ({ viewModel }) => {
    const basePath = useRelativePath('event-store');
    return (
        <div>
            <h1 className='text-4xl m-3'>Select Event Store</h1>
            <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 '>
                {viewModel.eventStores.map((eventStore) => {
                    return (
                        <StoreCard
                            key={eventStore}
                            title={eventStore}
                            path={`${basePath}/${eventStore}/Default`}
                            footer={<CardFooter />}
                            description=''
                        />
                    );
                })}
            </div>
        </div >
    );
});
