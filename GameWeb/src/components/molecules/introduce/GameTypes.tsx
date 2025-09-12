import { Shiny } from '../../atoms/buttons/Shiny';

export const GameTypes = () => {
  const texts = ['멀티플레이', '3D', 'FPS', '전투'];
  return (
    <div className="flex flex-wrap gap-2">{texts && texts.map((t) => <Shiny text={t} />)}</div>
  );
};
