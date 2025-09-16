package org.ssafy.gamedataserver.security;

import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import lombok.RequiredArgsConstructor;
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
import org.ssafy.gamedataserver.dto.user.UserDTO;
import org.ssafy.gamedataserver.entity.user.Role;
import org.ssafy.gamedataserver.entity.user.User;
import org.ssafy.gamedataserver.repository.UserRepository;

import java.util.Collections;
import java.util.Map;

@RestController
@RequestMapping("/api/v1/auth")
@RequiredArgsConstructor
public class AuthController {
    private final AuthenticationManager authenticationManager;
    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtProvider jwtProvider;
    private final SessionVersionService sessionVersionService;

    // 회원가입
    @PostMapping("/signup")
    public ResponseEntity<ResponseDTO<Void>> signup(@RequestBody UserDTO request) {
        String username = request.getUsername();
        String password = passwordEncoder.encode(request.getPassword());

        boolean isAlreadyTaken = userRepository.existsByUsername(username);
        boolean shortPassword = password.length() < 8;
        if (isAlreadyTaken) {
            return new ResponseEntity<>(ResponseDTO.fail("이미 존재하는 아이디입니다.", HttpStatus.CONFLICT), HttpStatus.CONFLICT);
        }
        if (shortPassword) {
            return new ResponseEntity<>(ResponseDTO.fail("비밀번호가 8자리 미만입니다.", HttpStatus.BAD_REQUEST), HttpStatus.BAD_REQUEST);
        }

        User user = User.builder()
                .username(username)
                .password(password)
                .roles(Collections.singleton(Role.USER))
                .build();

        userRepository.save(user);
        return ResponseEntity.ok(
                ResponseDTO.ok("회원가입 성공", null)
        );
    }

    // 로그인 → JWT 발급
    @PostMapping("/login")
    public ResponseEntity<ResponseDTO<Map<String, String>>> login(@RequestBody UserDTO request) {
        String username = request.getUsername();
        String password = request.getPassword();
        //로그인 검증
        try {
            authenticationManager.authenticate(
                    new UsernamePasswordAuthenticationToken(username, password)
            );

            long ver = sessionVersionService.setUserVersion(username);

            String accessToken = jwtProvider.generateToken(username, JwtProvider.TokenType.ACCESS, ver);
            String refreshToken = jwtProvider.generateToken(username, JwtProvider.TokenType.REFRESH, ver);
            return ResponseEntity.ok(
                    ResponseDTO.ok("로그인 성공", Map.of("accessToken", accessToken, "refreshToken", refreshToken))
            );

        } catch (BadCredentialsException e) {
            return ResponseEntity
                    .status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("아이디 또는 비밀번호가 틀렸습니다.", HttpStatus.UNAUTHORIZED));
        } catch (UsernameNotFoundException e) {
            return ResponseEntity
                    .status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("존재하지 않는 사용자입니다.", HttpStatus.UNAUTHORIZED));
        }
        // 내부적으로 이렇게 돌아감 authenticationManager ->
        // DaoAuthenticationProvider ->
        // 1. CustomUserDetailsService.loadUserByUsername(username) 호출,
        // 2. PawwsordEncoder.mathces(raw, encoded) 검증,
    }

    // 리프레시 토큰으로 엑세스 토큰 생성
    @PostMapping("/refresh")
    public ResponseEntity<ResponseDTO<Map<String, String>>> refreshToken(@RequestBody RefreshTokenDTO request) {
        String refreshToken = request.getRefreshToken();
        if (refreshToken == null || refreshToken.isBlank()) {
            return ResponseEntity
                    .badRequest()
                    .body(ResponseDTO.fail("refreshToken이 없습니다.", HttpStatus.BAD_REQUEST));
        }
        try {
            if (!jwtProvider.isRefreshToken(refreshToken)) {
                return ResponseEntity
                        .badRequest()
                        .body(ResponseDTO.fail("유효하지 않은 refreshToken 값입니다.", HttpStatus.UNAUTHORIZED));
            }
            String username = jwtProvider.getUsername(refreshToken);
            long tokenVer = jwtProvider.getVersion(refreshToken);
            long serverVer = sessionVersionService.getUserVersion(username);
            if (tokenVer != serverVer) {
                return ResponseEntity
                        .status(HttpStatus.UNAUTHORIZED)
                        .body(ResponseDTO.fail("다른 곳에서 해당 아이디로 로그인하여 해당 refresh 토큰이 유효하지 않습니다.", HttpStatus.UNAUTHORIZED));
            }
            String newAccess = jwtProvider.generateToken(username, JwtProvider.TokenType.ACCESS, serverVer);
            return ResponseEntity.ok(
                    ResponseDTO.ok("accessToken이 발급되었습니다.", Map.of("accessToken", newAccess))
            );
        } catch (ExpiredJwtException e) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("리프레시 토큰이 만료되었습니다.", HttpStatus.UNAUTHORIZED));
        } catch (JwtException e) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                    .body(ResponseDTO.fail("리프레시 토큰이 유효하지 않습니다.", HttpStatus.UNAUTHORIZED));
        }
    }
}
