import Fade from '../components/atoms/carousel/Fade';
import { DownloadButton } from '../components/molecules/download/DownloadButton';

export const TestPage = () => {
  return (
    <div className="flex flex-col items-center grow">
      <div className="max-w-1/2">
        <Fade />
      </div>
      <DownloadButton />
    </div>
  );
};
