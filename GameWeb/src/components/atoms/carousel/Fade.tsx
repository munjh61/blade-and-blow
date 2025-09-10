import Slider from 'react-slick'; // ✅ 올바른 import
import 'slick-carousel/slick/slick.css';
import 'slick-carousel/slick/slick-theme.css';
import { IMG_PNG } from '../../../types/Path';

function Fade() {
  const settings = {
    dots: true,
    fade: true,
    infinite: true,
    speed: 200,
    slidesToShow: 1,
    slidesToScroll: 1,
    waitForAnimate: false,
  };
  const images = ['pic1', 'pic2', 'pic3'];
  return (
    <div className="slider-container">
      <Slider {...settings}>
        {images.map((file, idx) => (
          <div className="aspect-[16/9] w-full">
            <img
              src={`${IMG_PNG}${file}.png`}
              alt={`slide-${idx + 1}`}
              className="object-cover w-full h-full"
            />
          </div>
        ))}
      </Slider>
    </div>
  );
}

export default Fade;
