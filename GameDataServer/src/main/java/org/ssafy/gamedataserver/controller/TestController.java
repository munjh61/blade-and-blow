package org.ssafy.gamedataserver.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.ssafy.gamedataserver.dto.ResponseDTO;

@RestController
@RequestMapping("/api/test")
public class TestController {
    @GetMapping("/ping")
    public ResponseEntity<ResponseDTO<Void>> test(){
        return ResponseEntity.ok(ResponseDTO.ok("test success",null));
    }
}
