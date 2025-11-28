import Logo from '../../assets/png/Game Logo.png';

export const Header = () => {
  return (
    <div className="flex justify-center bg-black">
      <img src={Logo} alt="logo" className="max-h-20" />
    </div>
  );
};
