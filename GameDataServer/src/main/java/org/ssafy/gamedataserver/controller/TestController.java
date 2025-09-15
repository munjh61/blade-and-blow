package org.ssafy.gamedataserver.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.ssafy.gamedataserver.dto.ResponseDTO;

import java.util.Map;

@RestController
@RequestMapping("/api/test")
public class TestController {
    @GetMapping("/ping")
    public ResponseEntity<ResponseDTO<Map<String,String>>> test(){
        String testData1 = "testData1";
        return ResponseEntity.ok(ResponseDTO.ok("test success", Map.of(testData1,testData1)));
    }
}
