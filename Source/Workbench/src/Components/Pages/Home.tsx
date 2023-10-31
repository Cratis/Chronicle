import { useLayoutContext } from '../../Layout/context/LayoutContext';
import { CardFooter } from '../Common/CardFooter';
import logoOpensjon from 'assets/icons/OpPeo.svg';
import { StoreCard } from '../Common/StoreCard';
import Innmeldt from 'assets/icons/cogInn.svg';
import uPlus from 'assets/icons/uPlus.png';
import { useEffect } from 'react';

export const Home = () => {
    const { toggleLeftSidebarOpen, layoutConfig } = useLayoutContext();

    useEffect(() => {
        toggleLeftSidebarOpen();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        !layoutConfig.leftSidebarHidden && (
            <div className=''>
                <h1 className='text-4xl m-3'>Select Event Store</h1>
                <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 '>
                    <StoreCard
                        title='Opensjon'
                        path='/opensjon'
                        logo={logoOpensjon}
                        footer={<CardFooter />}
                        description='Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur, aliquam deserunt. Dolorem eveniet officiis autem totam pariatur facilis, sapiente sed!'
                    />
                    <StoreCard
                        title='Innmeldt'
                        logo={Innmeldt}
                        footer={<CardFooter />}
                        description='Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur, aliquam deserunt. Dolorem eveniet officiis autem totam pariatur facilis, sapiente sed!'
                    />
                    <StoreCard
                        title='YouPlus'
                        logo={uPlus}
                        footer={<CardFooter />}
                        description='Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur, aliquam deserunt. Dolorem eveniet officiis autem totam pariatur facilis, sapiente sed!'
                    />
                </div>
            </div>
        )
    );
};
