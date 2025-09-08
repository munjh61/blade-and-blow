package org.ssafy.gamedataserver.security;

import lombok.RequiredArgsConstructor;
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
import org.ssafy.gamedataserver.entity.Role;
import org.ssafy.gamedataserver.entity.User;
import org.ssafy.gamedataserver.repository.UserRepository;

import java.util.Collections;
import java.util.Map;

@RestController
@RequestMapping("/api/auth")
@RequiredArgsConstructor
public class AuthController {
    private final AuthenticationManager authenticationManager;
    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtProvider jwtProvider;

    // 회원가입
    @PostMapping("/signup")
    public ResponseEntity<ResponseDTO<Void>> signup(@RequestBody Map<String, String> request) {
        String username = request.get("username");
        String password = passwordEncoder.encode(request.get("password"));

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
    public ResponseEntity<ResponseDTO<Map<String, String>>> login(@RequestBody Map<String, String> request) {
        String username = request.get("username");
        String password = request.get("password");
        //로그인 검증
        try {
            authenticationManager.authenticate(
                    new UsernamePasswordAuthenticationToken(username, password)
            );
            String token = jwtProvider.generateToken(username);
            return ResponseEntity.ok(
                    ResponseDTO.ok("로그인 성공", Map.of("accessToken", token))
            );

        } catch (BadCredentialsException e) {
            return ResponseEntity
                    .status(401)
                    .body(ResponseDTO.fail("아이디 또는 비밀번호가 잘못되었습니다."));
        } catch (UsernameNotFoundException e) {
            return ResponseEntity
                    .status(401)
                    .body(ResponseDTO.fail("존재하지 않는 사용자입니다."));
        }
        // 내부적으로 이렇게 돌아감 authenticationManager ->
        // DaoAuthenticationProvider ->
        // 1. CustomUserDetailsService.loadUserByUsername(username) 호출,
        // 2. PawwsordEncoder.mathces(raw, encoded) 검증,

    }
}
