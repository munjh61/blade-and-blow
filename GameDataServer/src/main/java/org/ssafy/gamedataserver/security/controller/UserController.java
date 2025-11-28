package org.ssafy.gamedataserver.security.controller;

import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.web.bind.annotation.*;
import org.ssafy.gamedataserver.dto.ResponseDTO;
import org.ssafy.gamedataserver.dto.user.UserMeDTO;
import org.ssafy.gamedataserver.dto.user.UserNicknameDTO;
import org.ssafy.gamedataserver.dto.user.UserSignUpDTO;
import org.ssafy.gamedataserver.entity.user.User;
import org.ssafy.gamedataserver.repository.UserRepository;

import java.util.Optional;

@RestController
@RequestMapping("/api/v1/user")
@RequiredArgsConstructor
public class UserController {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;

    // 내 정보 조회
    @GetMapping("/me")
    public ResponseEntity<ResponseDTO<UserMeDTO>> getMe() {
        Optional<User> op = getCurrentUser();
        if (op.isEmpty()) {
            return ResponseDTO.notFound("user does not exist");
        }
        User user = op.get();
        UserMeDTO dto = new UserMeDTO();
        dto.setUsername(user.getUsername());
        dto.setNickname(user.getNickname());
        return ResponseDTO.ok("Got user Information Successfully!", dto);
    }

    // 닉네임 변경
    @PutMapping("/nickname")
    public ResponseEntity<ResponseDTO<Void>> changeNickname(@RequestBody UserNicknameDTO req) {
        Optional<User> op = getCurrentUser();
        if (op.isEmpty()) {
            return ResponseDTO.notFound("user does not exist");
        }
        User user = op.get();
        user.setNickname(req.getNickname());
        userRepository.save(user);
        return ResponseDTO.ok("nickname changed successfully", null);
    }

    // 게스트 → 일반 유저 전환
    @PostMapping("/guestasuser")
    public ResponseEntity<ResponseDTO<Void>> guestToSignupUser(@RequestBody UserSignUpDTO req) {
        // 가드: 아이디 중복 / 비밀번호 길이
        if (userRepository.existsByUsername(req.getUsername())) {
            return ResponseDTO.conflict("ID already exist");
        }
        if (req.getPassword() == null || req.getPassword().length() < 8) {
            return ResponseDTO.badRequest("Password is has to be longer than 8 letters");
        }

        Optional<User> op = getCurrentUser();
        if (op.isEmpty()) {
            return ResponseDTO.notFound("user does not exist");
        }
        User user = op.get();
        user.setUsername(req.getUsername());
        user.setPassword(passwordEncoder.encode(req.getPassword()));
        user.setNickname(req.getNickname());
        userRepository.save(user);
        return ResponseDTO.ok("guest account created successfully", null);
    }

    // 현재 인증 사용자 조회
    private Optional<User> getCurrentUser() {
        Authentication auth = SecurityContextHolder.getContext().getAuthentication();
        if (auth == null || auth.getName() == null) return Optional.empty();
        return userRepository.findByUsername(auth.getName());
    }
}
