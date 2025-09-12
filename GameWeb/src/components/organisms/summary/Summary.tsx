import { DownloadButton } from '../../molecules/download/DownloadButton';
import { GameTypes } from '../../molecules/introduce/GameTypes';
import { Introduce } from '../../molecules/introduce/Introduce';

export const Summary = () => {
  return (
    <div className="flex flex-col grow p-2 gap-2">
      <Introduce />
      <GameTypes />
      <DownloadButton />
    </div>
  );
};
