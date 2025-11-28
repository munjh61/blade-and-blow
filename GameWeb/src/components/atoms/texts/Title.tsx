import type { TextProps } from '../../../types/Props';

export const Title = ({ text }: TextProps) => {
  return <span className="text-white text-fluid-lg">{text}</span>;
};
