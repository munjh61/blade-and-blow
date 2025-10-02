package org.ssafy.gamedataserver.security.controller;

import java.time.Duration;
import java.util.*;
import java.util.stream.Collectors;

import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.http.ResponseEntity;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.web.bind.annotation.*;
import org.ssafy.gamedataserver.dto.ResponseDTO;
import org.ssafy.gamedataserver.dto.user.LoginDTO;
import org.ssafy.gamedataserver.dto.user.UserDTO;
import org.ssafy.gamedataserver.dto.user.UserSignUpDTO;
import org.ssafy.gamedataserver.entity.user.Role;
import org.ssafy.gamedataserver.entity.user.User;
import org.ssafy.gamedataserver.repository.UserRepository;

import jakarta.servlet.http.HttpServletRequest;
import org.ssafy.gamedataserver.security.JwtProvider;
import org.ssafy.gamedataserver.security.SessionVersionService;
import org.ssafy.gamedataserver.security.dto.RefreshTokenDTO;

@Slf4j
@RestController
@RequestMapping("/api/v1/auth")
@RequiredArgsConstructor
public class AuthController {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtProvider jwtProvider;
    private final SessionVersionService sessionVersionService;
    private final StringRedisTemplate redis;

    /* ====================== Sign Up ====================== */

    @PostMapping("/signup")
    public ResponseEntity<ResponseDTO<Void>> signup(@RequestBody UserSignUpDTO req) {
        final String username = req.getUsername();
        final String rawPassword = req.getPassword();
        final String nickname = req.getNickname();

        if (userRepository.existsByUsername(username)) {
            return ResponseDTO.conflict("ID already exist");
        }
        if (rawPassword == null || rawPassword.length() < 8) {
            return ResponseDTO.badRequest("Password is has to be longer than 8 letters");
        }

        final String encoded = passwordEncoder.encode(rawPassword);
        final User user = User.builder()
                .username(username)
                .password(encoded)
                .nickname(nickname)
                .roles(Collections.singleton(Role.USER))
                .build();

        userRepository.save(user);
        return ResponseDTO.ok("Welcome!", null);
    }

    /* ====================== Login ====================== */

    @PostMapping("/login")
    public ResponseEntity<ResponseDTO<LoginDTO>> login(@RequestBody UserDTO req, HttpServletRequest httpReq) {
        final String username = req.getUsername();
        final String password = req.getPassword();
        final String mac = req.getMac();

        // 한 디바이스 1세션: 디바이스 버전 증가
        final long deviceVer = sessionVersionService.setDeviceVersion(mac);

        final Optional<User> op = userRepository.findByUsername(username);
        if (op.isEmpty() || !passwordEncoder.matches(password, op.get().getPassword())) {
            return ResponseDTO.unauthorized("Wrong ID or Password");
        }

        final User user = op.get();
        final long id = user.getId();
        final long ver = sessionVersionService.setUserVersion(id);     // 한 계정 1세션
        final Set<Role> roles = user.getRoles();

        // 토큰 생성
        final String accessToken = jwtProvider.generateToken(id, username, roles, JwtProvider.TokenType.ACCESS, ver, mac, deviceVer);
        final String refreshToken = jwtProvider.generateToken(id, username, roles, JwtProvider.TokenType.REFRESH, ver, mac, deviceVer);

        // 응답 DTO
        final LoginDTO dto = new LoginDTO();
        dto.setUsername(username);
        dto.setNickname(user.getNickname());
        dto.setAccessToken(accessToken);
        dto.setRefreshToken(refreshToken);

        // Redis 세션 기록
        writeRedisSession(username, ver, httpReq);

        return ResponseDTO.ok("Login successful", dto);
    }

    /* ====================== Refresh ====================== */

    @PostMapping("/refresh")
    public ResponseEntity<ResponseDTO<Map<String, String>>> refreshToken(@RequestBody RefreshTokenDTO req) {
        final String refreshToken = req.getRefreshToken();
        if (refreshToken == null || refreshToken.isBlank()) {
            return ResponseDTO.badRequest("No Refresh Token");
        }

        try {
            if (!jwtProvider.isRefreshToken(refreshToken)) {
                return ResponseDTO.unauthorized("Is Not Refresh Token");
            }

            final Long id = jwtProvider.getUserId(refreshToken);
            final String username = jwtProvider.getUsername(refreshToken);
            final Set<Role> roles = jwtProvider.getRoles(refreshToken).stream().map(Role::valueOf).collect(Collectors.toSet());
            final long tokenVer = jwtProvider.getVersion(refreshToken);
            final long serverVer = sessionVersionService.getUserVersion(id);

            final String mac = jwtProvider.getMac(refreshToken);
            if (mac == null || mac.isBlank()) {
                return ResponseDTO.unauthorized("No MAC id");
            }
            final long tokenDeviceVer = jwtProvider.getDeviceVersion(refreshToken);
            final long serverDeviceVer = sessionVersionService.getDeviceVersion(mac);

            if (tokenVer != serverVer) {
                return ResponseDTO.unauthorized("Somebody Log in with your ID");
            }
            if (tokenDeviceVer != serverDeviceVer) {
                return ResponseDTO.unauthorized("You had login with other account");
            }

            final String newAccess = jwtProvider.generateToken(id, username, roles, JwtProvider.TokenType.ACCESS, serverVer, mac, serverDeviceVer);
            return ResponseDTO.ok("Got a New accessToken", Map.of("accessToken", newAccess));

        } catch (ExpiredJwtException e) {
            return ResponseDTO.unauthorized("Refresh Token Expired");
        } catch (JwtException e) {
            return ResponseDTO.unauthorized("Refresh Token Not Valid");
        }
    }

    /* ====================== Redis ====================== */

    private void writeRedisSession(String username, long ver, HttpServletRequest req) {
        try {
            final long expireMinutes = 60;
            final String jti = UUID.randomUUID().toString(); // 참고: 현재 JWT에는 jti 미포함

            final String ip = req.getRemoteAddr();
            final String ua = req.getHeader("User-Agent");

            final String sessionKey = "session:" + jti;
            final String sessionJson = String.format(
                    "{\"user\":\"%s\",\"ver\":%d,\"exp\":%d,\"ip\":\"%s\",\"ua\":\"%s\"}",
                    username, ver, expireMinutes, ip, ua
            );

            redis.opsForValue().set(sessionKey, sessionJson, Duration.ofMinutes(expireMinutes));

            final String userSessionKey = "user:" + username + ":sessions";
            redis.opsForSet().add(userSessionKey, jti);
            redis.expire(userSessionKey, Duration.ofMinutes(expireMinutes));
        } catch (Exception e) {
            log.warn("Failed to write Redis session: {}", e.getMessage());
        }
    }
}
