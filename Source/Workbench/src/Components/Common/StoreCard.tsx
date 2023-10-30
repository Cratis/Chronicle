import { Image } from 'primereact/image';
import { Card } from 'primereact/card';

export interface IStoreCard {
    logo?: string;
    title?: string;
    description?: string;
    footer?: React.ReactNode;
}

export function StoreCard(props: IStoreCard) {
    const { logo, title, footer, description } = props;
    const image = (
        <div className='w-24 h-24 '>
            <Image  alt='Card' src={logo} />
        </div>
    );
    const heading = <h1 className='text-2xl'> {title}</h1>;

    return (
        <div className='m-4'>
            <Card
                className='flex p-2 border-2 shadow-none'
                title={heading}
                footer={footer}
                header={image}
            >
                {description}
            </Card>
        </div>
    );
}
