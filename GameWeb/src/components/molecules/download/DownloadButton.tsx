import { IoMdDownload } from 'react-icons/io';
import { Dots } from '../../atoms/buttons/Dots';
import { BASE_URL } from '../../../types/Path';

const FILE_URL = `${BASE_URL}/download/BladeAndBlow.zip`;

export const DownloadButton = () => {
  const handleDownload = () => {
    const a = document.createElement('a');
    a.href = FILE_URL;
    a.download = 'BladeAndBlow.zip';
    document.body.appendChild(a);
    a.click();
    a.remove();
  };

  return (
    <Dots onClick={handleDownload} icon={IoMdDownload}>
      다운로드
    </Dots>
  );
};
