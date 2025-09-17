package org.ssafy.gamedataserver.controller.test;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.ssafy.gamedataserver.dto.ResponseDTO;

import java.util.Map;

@RestController
@RequestMapping("/api/v1/public/test")
public class TestPublicController {
    @GetMapping("/ping")
    public ResponseEntity<ResponseDTO<Map<String,String>>> test(){
        String msg = "connect success";
        return ResponseEntity.ok(ResponseDTO.ok("test success", Map.of("msg",msg)));
    }
}
