import { Shiny } from '../../atoms/buttons/Shiny';

export const GameTypes = () => {
  const texts = ['멀티플레이', '3D', 'TPS', '전투', '엽기'];
  return (
    <div className="flex flex-wrap gap-2">
      {texts && texts.map((t, idx) => <Shiny key={idx} text={t} />)}
    </div>
  );
};
