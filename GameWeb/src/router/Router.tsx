import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { OnBoardingPage } from '../Page/OnBoardingPage';
import { MainPage } from '../Page/MainPage';
import { TestPage } from '../Page/TestPage';

function RootRedirect() {
  const isLoggedIn = true;
  return isLoggedIn ? <Navigate to="/main" replace /> : <OnBoardingPage />;
}
const PrivateRoute = () => {
  const isLoggedIn = true;
  return isLoggedIn ? <Outlet /> : <Navigate to="/login" replace />;
};

export const Router = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* 로그인 여부에 따라 온보딩 or 메인 페이지 이동 */}
        <Route path="/" element={<RootRedirect />} />

        {/* 권한 */}
        <Route element={<PrivateRoute />}>
          <Route path="/main" element={<MainPage />} />
          <Route path="/test" element={<TestPage />} />
        </Route>

        {/* 잘못된 경로 */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
};
