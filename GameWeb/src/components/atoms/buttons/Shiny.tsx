import type { TextProps } from '../../../types/Props';

export const Shiny = ({ text }: TextProps) => {
  return (
    <button
      className="
      bg-brand-red text-white text-fluid-sm
        group relative overflow-hidden
        p-2 rounded-md
        min-w-15
      "
    >
      {text}
      <span
        className="
          absolute top-1/2 -translate-y-1/2
          left-[-50px] w-5 h-[110%] bg-white opacity-0
          rotate-[30deg]
          transition-all duration-[1000ms] ease-[cubic-bezier(0.23,1,0.32,1)]
          group-hover:left-[calc(100%+50px)] group-hover:opacity-100
        "
      />
    </button>
  );
};
