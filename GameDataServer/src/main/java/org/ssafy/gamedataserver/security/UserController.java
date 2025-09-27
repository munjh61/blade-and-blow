package org.ssafy.gamedataserver.security;

import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.*;
import org.ssafy.gamedataserver.dto.ResponseDTO;
import org.ssafy.gamedataserver.dto.user.UserMeDTO;
import org.ssafy.gamedataserver.dto.user.UserNicknameDTO;
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
    public ResponseEntity<ResponseDTO<UserMeDTO>> getMe() {
        if (getCurrentUser().isPresent()) {
            User user = getCurrentUser().get();
            UserMeDTO dto = new UserMeDTO();
            dto.setUsername(user.getUsername());
            dto.setNickname(user.getNickname());
            return ResponseEntity.ok(ResponseDTO.ok("Got user Information Successfully!", dto));
        }
        return ResponseEntity
                .status(HttpStatus.NOT_FOUND)
                .body(ResponseDTO.fail("user does not exist", HttpStatus.NOT_FOUND));
    }

    // 닉네임 변경
    @PutMapping("/nickname")
    public ResponseEntity<ResponseDTO<Void>> changeNickname(@RequestBody UserNicknameDTO nickname) {
        if (getCurrentUser().isPresent()) {
            User user = getCurrentUser().get();
            user.setNickname(nickname.getNickname());
            userRepository.save(user);
            return ResponseEntity.ok(ResponseDTO.ok("nickname changed successfully", null));
        }
        return ResponseEntity
                .status(HttpStatus.NOT_FOUND)
                .body(ResponseDTO.fail("user does not exist", HttpStatus.NOT_FOUND));
    }

    // 조회
    private Optional<User> getCurrentUser() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        String username = authentication.getName();
        return userRepository.findByUsername(username);
    }
}
