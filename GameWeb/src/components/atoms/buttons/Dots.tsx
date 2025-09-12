import type { ButtonProps } from '../../../types/Props';
import { cn } from '../../../utils/cn';
export const Dots = ({ onClick, children, icon: Icon }: ButtonProps) => {
  return (
    <button
      onClick={onClick}
      className={cn(
        'w-full flex items-center justify-center min-w-0',
        'cursor-pointer',
        'bg-purple-900 hover:bg-purple-900/90',
        'py-3 px-6 uppercase tracking-wider rounded relative overflow-hidden group transition-all duration-300',
      )}
    >
      {Icon && <Icon color="white" />}
      <span className="text-white font-hakgyo">{children}</span>
      <div className="absolute inset-0 transition-opacity duration-300 opacity-0 group-hover:opacity-100">
        {[...Array(6)].map((_, i) => (
          <div
            key={i}
            className="absolute w-3 h-3 border-2 border-purple-300 rounded-full animate-ping"
            style={{
              left: `${15 + i * 12}%`,
              top: '50%',
              transform: 'translateY(-50%)',
              animationDelay: `${i * 0.1}s`,
            }}
          />
        ))}
      </div>
    </button>
  );
};
