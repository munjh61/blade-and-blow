import type { TextProps } from '../../../types/Props';

export const MainDescription = ({ text }: TextProps) => {
  return <span className="text-fluid-lg font-bold text-brand-purple">{text}</span>;
};
