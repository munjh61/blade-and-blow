import { cva } from 'class-variance-authority';

export const Variants = cva('inline-flex items-center justify-center rounded-md', {
  variants: {
    hover: {
      active: 'hover:opacity-90 active:scale-95 active:shadow-inner cursor-pointer',
    },
  },
});
