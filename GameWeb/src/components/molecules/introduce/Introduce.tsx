import type { SubTitleAndContent } from '../../../types/Props';
import { MainDescription } from '../../atoms/texts/MainDescription';
import { SubContent } from '../../atoms/texts/SubContent';
import { Subtitle } from '../../atoms/texts/Subtitle';

const TC = ({ st, sc }: SubTitleAndContent) => {
  return (
    <div className="flex justify-between px-[10%]">
      <Subtitle text={st} />
      <SubContent text={sc} />
    </div>
  );
};

export const Introduce = () => {
  const description = `당신은 어떤 무기를 선택해, 어떤 전략으로 최후의 1인이 될 것인가?
    무기의 특성과 상성을 활용해 상대를 제압하고, 마지막까지 살아남아보세요.`;
  return (
    <div className="grow flex flex-col justify-evenly">
      <MainDescription text={description} />
      <div className="flex flex-col justify-center">
        <TC st="예정일" sc="2025. 09. 31" />
        <TC st="배포자" sc="SSAFY13_405_겜알못들" />
      </div>
    </div>
  );
};
