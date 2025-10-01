package org.ssafy.gamedataserver.security;

import java.time.Duration;
import java.util.Collections;
import java.util.Map;
import java.util.NoSuchElementException;
import java.util.UUID;
import org.springframework.data.redis.core.StringRedisTemplate;
import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.ssafy.gamedataserver.dto.ResponseDTO;
import org.ssafy.gamedataserver.dto.user.LoginDTO;
import org.ssafy.gamedataserver.dto.user.UserDTO;
import org.ssafy.gamedataserver.dto.user.UserSignUpDTO;
import org.ssafy.gamedataserver.entity.user.Role;
import org.ssafy.gamedataserver.entity.user.User;
import org.ssafy.gamedataserver.repository.UserRepository;

import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;

@Slf4j
@RestController
@RequestMapping("/api/v1/auth")
@RequiredArgsConstructor
public class AuthController {
    private final AuthenticationManager authenticationManager;
    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtProvider jwtProvider;
    private final SessionVersionService sessionVersionService;
    private final StringRedisTemplate redis;
    
    // 회원가입
    @PostMapping("/signup")
    public ResponseEntity<ResponseDTO<Void>> signup(@RequestBody UserSignUpDTO request) {
        String username = request.getUsername();
        String password = passwordEncoder.encode(request.getPassword());
        String nickname = request.getNickname();

        boolean isAlreadyTaken = userRepository.existsByUsername(username);
        boolean shortPassword = request.getPassword().length() < 8;
        if (isAlreadyTaken) {
            return new ResponseEntity<>(ResponseDTO.fail("ID already exist", HttpStatus.CONFLICT), HttpStatus.CONFLICT);
        }
        if (shortPassword) {
            return new ResponseEntity<>(ResponseDTO.fail("Password is has to be longer than 8 letters", HttpStatus.BAD_REQUEST), HttpStatus.BAD_REQUEST);
        }

        User user = User.builder()
                .username(username)
                .password(password)
                .nickname(nickname)
                .roles(Collections.singleton(Role.USER))
                .build();

        userRepository.save(user);
        return ResponseEntity.ok(
                ResponseDTO.ok("Welcome!", null)
        );
    }

    // 로그인 → JWT 발급
    @PostMapping("/login")
    public ResponseEntity<ResponseDTO<LoginDTO>> login(@RequestBody UserDTO request, HttpServletRequest httpreq) {

        String username = request.getUsername();
        String password = request.getPassword();
        
        //로그인 검증
        try {
            authenticationManager.authenticate(
                    new UsernamePasswordAuthenticationToken(username, password)
            );

            // JWT, Redis에 넣기 위한 변수 초기화
            long ver = sessionVersionService.setUserVersion(username);
            String jti = UUID.randomUUID().toString();
            long expireMinutes = 60;
            
            String ip = httpreq.getRemoteAddr();
            String ua = httpreq.getHeader("User-Agent");
            
            // Token 생성 시, jti 반영해야 함.
            String accessToken = jwtProvider.generateToken(username, JwtProvider.TokenType.ACCESS, ver);
            String refreshToken = jwtProvider.generateToken(username, JwtProvider.TokenType.REFRESH, ver);

            //Redis에 로그인 세션 관리를 위한 데이터 관리
            String sessionKey = "session:" + jti;
            String sessionJson = String.format(
            		"{\"user\":\"%s\",\"ver\":%d,\"exp\":%d,\"ip\":\"%s\",\"ua\":\"%s\"}",
            		username, ver, expireMinutes, ip, ua
            		);
            
            // Set으로 하나의 Session만 등록하게 함.
            redis.opsForValue().set(sessionKey, sessionJson, Duration.ofMinutes(expireMinutes));
            
            // User별 session 관리
            String userSessionKey = "user:"+ username + ":sessions";
            redis.opsForSet().add(userSessionKey, jti);
            redis.expire(userSessionKey, Duration.ofMinutes(expireMinutes));
            
            User user = userRepository.findByUsername(username).get();
            String nickname = user.getNickname();

            LoginDTO loginDTO = new LoginDTO();
            loginDTO.setUsername(username);
            loginDTO.setNickname(nickname);
            loginDTO.setAccessToken(accessToken);
            loginDTO.setRefreshToken(refreshToken);


            return ResponseEntity.ok(
                    ResponseDTO.ok("Login successful", loginDTO)
            );

        } catch (BadCredentialsException e) {
            return ResponseEntity
                    .status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("ID or Password is wrong", HttpStatus.UNAUTHORIZED));
        } catch (UsernameNotFoundException | NoSuchElementException e) {
            return ResponseEntity
                    .status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("ID or Password is wrong", HttpStatus.UNAUTHORIZED)); // 아이디가 없는 거지만, 티를 내면 안됨
        }
        // 내부적으로 이렇게 돌아감 authenticationManager ->
        // DaoAuthenticationProvider ->
        // 1. CustomUserDetailsService.loadUserByUsername(username) 호출,
        // 2. PasswordEncoder.matches(raw, encoded) 검증,
    }

    // 리프레시 토큰으로 엑세스 토큰 생성
    @PostMapping("/refresh")
    public ResponseEntity<ResponseDTO<Map<String, String>>> refreshToken(@RequestBody RefreshTokenDTO request) {
        String refreshToken = request.getRefreshToken();
        if (refreshToken == null || refreshToken.isBlank()) {
            return ResponseEntity
                    .badRequest()
                    .body(ResponseDTO.fail("No Refresh Token", HttpStatus.BAD_REQUEST));
        }
        try {
            if (!jwtProvider.isRefreshToken(refreshToken)) {
                return ResponseEntity
                        .badRequest()
                        .body(ResponseDTO.fail("Is Not Refresh Token", HttpStatus.UNAUTHORIZED));
            }
            String username = jwtProvider.getUsername(refreshToken);
            long tokenVer = jwtProvider.getVersion(refreshToken);
            long serverVer = sessionVersionService.getUserVersion(username);
            if (tokenVer != serverVer) {
                return ResponseEntity
                        .status(HttpStatus.UNAUTHORIZED)
                        .body(ResponseDTO.fail("Somebody Log in with your ID", HttpStatus.UNAUTHORIZED));
            }
            String newAccess = jwtProvider.generateToken(username, JwtProvider.TokenType.ACCESS, serverVer);
            return ResponseEntity.ok(
                    ResponseDTO.ok("Got a New accessToken", Map.of("accessToken", newAccess))
            );
        } catch (ExpiredJwtException e) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("Refresh Token Expired", HttpStatus.UNAUTHORIZED));
        } catch (JwtException e) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("Refresh Token Not Valid", HttpStatus.UNAUTHORIZED));
        }
    }

}
