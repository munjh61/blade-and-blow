import Fade from '../atoms/carousel/Fade';
import { Summary } from '../organisms/summary/Summary';

export const MainTemplate = () => {
  return (
    <div className="grid md:grid-cols-1 xl:grid-cols-2 w-full gap-2">
      <Fade className="max-w-dvw" />
      <Summary />
    </div>
  );
};
