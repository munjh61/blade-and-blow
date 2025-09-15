import axios from 'axios';
import { Header } from '../components/common/Header';
import { MainTemplate } from '../components/templates/MainTemplate';
import { BASE_URL } from '../types/Path';

export const MainPage = () => {
  const test = () => {
    const res = axios.get(`${BASE_URL}api/test/ping`);
    res.then((res) => console.log(res));
  };
  return (
    <div>
      <Header />
      <MainTemplate />

      <button onClick={test} className="text-white bg-brand-blue">
        버튼
      </button>
    </div>
  );
};
