import type { MouseEventHandler, ReactNode } from 'react';
import type { Variants } from './Variants';
import type { VariantProps } from 'class-variance-authority';
import type { IconType } from 'react-icons';

export type ButtonProps = {
  onClick?: MouseEventHandler<HTMLButtonElement>;
  children?: ReactNode;
  className?: string;
  icon?: IconType;
} & VariantProps<typeof Variants>;

export type TextProps = {
  text?: string;
  className?: string;
};

export type SubTitleAndContent = {
  st: string;
  sc: string;
};
