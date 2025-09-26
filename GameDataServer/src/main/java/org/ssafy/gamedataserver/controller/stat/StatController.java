package org.ssafy.gamedataserver.controller.stat;

import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.ssafy.gamedataserver.dto.ResponseDTO;
import org.ssafy.gamedataserver.dto.stat.StatModeDTO;
import org.ssafy.gamedataserver.entity.battle.Mode;
import org.ssafy.gamedataserver.service.StatService;

@RestController
@RequestMapping("/api/v1/stat")
@RequiredArgsConstructor
public class StatController {
    StatService statService;

    @GetMapping("/{mode}")
    public ResponseEntity<ResponseDTO<StatModeDTO>> getStat(@PathVariable Mode mode) {
        statService.init();
        StatModeDTO statModeDTO = statService.getStat(mode);
        return ResponseEntity.ok(ResponseDTO.ok("Successfully Got Battle Stat", statModeDTO));
    }
}
