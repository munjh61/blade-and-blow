package org.ssafy.gamedataserver.security;

import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.*;
import org.ssafy.gamedataserver.dto.ResponseDTO;
import org.ssafy.gamedataserver.entity.user.User;
import org.ssafy.gamedataserver.repository.UserRepository;

import java.util.Optional;

@RestController
@RequestMapping("/api/v1/user")
@RequiredArgsConstructor
public class UserController {
    private final UserRepository userRepository;

    // 닉네임 조회
    @GetMapping("/me")
    public ResponseEntity<ResponseDTO<String>> getNickname() {
        if (getCurrentUser().isPresent()) {
        User user = getCurrentUser().get();
        return ResponseEntity.ok(ResponseDTO.ok("본인 조회 성공", user.getNickname()));
        }
        return ResponseEntity
                .status(HttpStatus.NOT_FOUND)
                .body(ResponseDTO.fail("해당 유저가 존재하지 않습니다.",HttpStatus.NOT_FOUND));
    }

    // 닉네임 변경
    @PostMapping("/nickname")
    public ResponseEntity<ResponseDTO<Void>> changeNickname(@RequestParam String nickname) {
        if (getCurrentUser().isPresent()) {
            User user = getCurrentUser().get();
            user.setNickname(nickname);
            userRepository.save(user);
            return ResponseEntity.ok(ResponseDTO.ok("닉네임이 변경되었습니다.", null));
        }
        return ResponseEntity
                .status(HttpStatus.NOT_FOUND)
                .body(ResponseDTO.fail("해당 유저가 존재하지 않습니다.",HttpStatus.NOT_FOUND));
    }

    // 조회
    private Optional<User> getCurrentUser() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        String username = authentication.getName();
        return userRepository.findByUsername(username);
    }
}
