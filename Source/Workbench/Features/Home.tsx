// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CardFooter } from '../Components/Common/CardFooter';
import logoOpensjon from 'assets/icons/OpPeo.svg';
import { StoreCard } from '../Components/Common/StoreCard';
import Innmeldt from 'assets/icons/cogInn.svg';
import uPlus from 'assets/icons/uPlus.png';

import { tw } from 'typewind';

export const Home = () => {

    return (
        <div>
            <h1 className={tw.text_4xl.m_3}>Select Event Store</h1>
            <div className={`${tw.grid.grid_cols_2} md:${tw.grid_cols_2} lg:${tw.grid_cols_2}`}>
                <StoreCard
                    title='Opensjon'
                    path='/event-store/opensjon'
                    logo={logoOpensjon}
                    footer={<CardFooter/>}
                    description='Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur, aliquam deserunt. Dolorem eveniet officiis autem totam pariatur facilis, sapiente sed!'
                />
                <StoreCard
                    title='Innmeldt'
                    logo={Innmeldt}
                    footer={<CardFooter/>}
                    description='Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur, aliquam deserunt. Dolorem eveniet officiis autem totam pariatur facilis, sapiente sed!'
                />
                <StoreCard
                    title='YouPlus'
                    logo={uPlus}
                    footer={<CardFooter/>}
                    description='Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur, aliquam deserunt. Dolorem eveniet officiis autem totam pariatur facilis, sapiente sed!'
                />
            </div>
        </div>
    );
};
