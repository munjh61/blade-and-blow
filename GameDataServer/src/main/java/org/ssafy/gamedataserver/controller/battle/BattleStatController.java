package org.ssafy.gamedataserver.controller.battle;

import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.ssafy.gamedataserver.dto.ResponseDTO;
import org.ssafy.gamedataserver.dto.stat.BattleStatModeDTO;
import org.ssafy.gamedataserver.entity.battle.Mode;
import org.ssafy.gamedataserver.service.BattleStatService;

@RestController
@RequestMapping("/api/v1/stat")
@RequiredArgsConstructor
public class BattleStatController {
    BattleStatService battleStatService;

    @GetMapping("/{mode}")
    public ResponseEntity<ResponseDTO<BattleStatModeDTO>> getStat(@PathVariable Mode mode) {
        battleStatService.init();
        BattleStatModeDTO battleStatModeDTO = battleStatService.getStat(mode);
        return ResponseEntity.ok(ResponseDTO.ok("Successfully Got Battle Stat", battleStatModeDTO));
    }
}
