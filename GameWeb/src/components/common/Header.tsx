import Logo1 from '../../../src/assets/png/Logo_mini.png';
import Logo2 from '../../../src/assets/png/Logo_mini_long.png';

export const Header = () => {
  return (
    <div className="flex justify-between bg-black">
      <picture>
        <source srcSet={Logo2} media="(min-width: 1024px)" />
        <img src={Logo1} className="max-h-20 w-full" alt="Logo" />
      </picture>
    </div>
  );
};
