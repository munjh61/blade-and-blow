package org.ssafy.gamedataserver.controller.ingame;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.ssafy.gamedataserver.dto.ingame.DamageDto;
import org.ssafy.gamedataserver.service.ingame.InGameService;

import io.swagger.v3.oas.annotations.Operation;

// End-point URL 막아놓기
@RestController
@RequestMapping("/api/v1/ingame")
public class InGameController {
	
	// Service Instance 주입
	private final InGameService service;
	
	public InGameController(InGameService service) {
		this.service = service;
	}
	
	@GetMapping("/test")
	public String test(){
		return "Test Succeeded";
	}
	
	@PostMapping("/killSave")
	@Operation(summary = "히트 데이터 Redis에 저장" , description = "Damage를 입히는 요청 -> Redis로 송신")
	public ResponseEntity<?> postHit(@RequestBody DamageDto damageRequest){
		
		service.saveRedis(damageRequest);
		
		return ResponseEntity.ok("Saved in Redis");
	}
	
	// Leaderboard
	// 게임 전체에 대한 정보
	// 최대 사용 무기
	// 사용자 이용 시간 대
	@GetMapping("/gameInfo")
	@Operation(summary = "게임 전체 유저에 대한 정보" , description = " 게임 전체에서 최대 사용 무기, 사용자 주 사용 시간대 등 ")
	public ResponseEntity<?> getHit(){
		
		service.getGameInfo();
		
		return null;
	}
	
	
}
