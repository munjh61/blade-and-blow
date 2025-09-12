import Slider from 'react-slick';
import 'slick-carousel/slick/slick.css';
import 'slick-carousel/slick/slick-theme.css';
import type { TextProps } from '../../../types/Props';
import pic1 from '../../../assets/png/pic1.png';
import pic2 from '../../../assets/png/pic2.png';
import pic3 from '../../../assets/png/pic3.png';

function Fade({ className }: TextProps) {
  const settings = {
    fade: true,
    infinite: true,
    arrows: false,
    speed: 1000,
    slidesToShow: 1,
    slidesToScroll: 1,
    centerMode: true,
    waitForAnimate: false,
    autoplay: true,
    autoplayspeed: 1,
    pauseOnHover: true,
  };
  const images = [pic1, pic2, pic3];
  return (
    <div className={className}>
      <div className="slider-container">
        <Slider {...settings}>
          {images.map((pic, idx) => (
            <div>
              <img src={pic} alt={`slide-${idx + 1}`} />
            </div>
          ))}
        </Slider>
      </div>
    </div>
  );
}

export default Fade;
