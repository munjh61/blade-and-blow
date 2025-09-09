/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          red: '#FF3245',
          orange: '#FF6940',
          yellow: '#FFA600',
          green: '#6DC16A',
          blue: '#6083EE',
          purple: '#9D5EEE',
        },
      },
      fontFamily: {
        sans: ['Pretendard', 'system-ui', 'sans-serif'],
        hakgyo: ['hakgyo', 'sans-serif'],
        outline: ['sds-outline', 'sans-serif'],
      },
      fontSize: {
        'fluid-sm': ['clamp(0.875rem, 0.8vw + 0.55rem, 1rem)', { lineHeight: '1.6' }],
        'fluid-md': ['clamp(1rem,     1.0vw + 0.6rem,  1.25rem)', { lineHeight: '1.6' }],
        'fluid-lg': ['clamp(1.125rem, 1.2vw + 0.6rem,  1.5rem)', { lineHeight: '1.5' }],
        'fluid-xl': ['clamp(1.25rem,  1.6vw + 0.6rem,  2rem)', { lineHeight: '1.3' }],
      },
    },
  },
  plugins: [],
};
