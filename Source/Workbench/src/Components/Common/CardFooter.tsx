import { FaRotate, FaXmark, FaWhmcs } from 'react-icons/fa6';

export const CardFooter = () => {
    return (
        <div className='flex justify-end gap-2'>
            <div className='flex gap-1'>
               5  <FaXmark size={25} color='red' />
            </div>
            <div className='flex gap-1'>
                15 <FaRotate size={25} color='blue'/>
            </div>
            <div className='flex gap-1'>
                <FaWhmcs size={25} />
            </div>
        </div>
    );
};
