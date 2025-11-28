package org.ssafy.gamedataserver.service.ingame;

import java.time.Duration;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Service;
import org.ssafy.gamedataserver.dto.ingame.DamageDto;

@Service
public class InGameService {
	
	private static final String GAME_HIT_PREFIX = "game:prod:kill:";
	
	private final MongoTemplate mongoTemplate;
	
	public InGameService(MongoTemplate mongoTemplate) {
		this.mongoTemplate = mongoTemplate;
	}
	
	/**
	 *RedisTemplate
	 *RedisTemplate<Key:String, Value:String> 
	**/
	@Autowired
	private StringRedisTemplate redis;	
	
	/**
	 * Redis에 게임 중 Kill 정보 보내기
	 * **/
	public void saveRedis(DamageDto req) {
		//Key 값
		String key = String.format(GAME_HIT_PREFIX + "%s:%s",
				req.getMatchId(), req.getAttackerId());
		
		// timestamp 존재 여부 확인
		long timeStamp = req.getTimeStamp()!= null && req.getTimeStamp()>0 ? req.getTimeStamp() : System.currentTimeMillis() ;
		
		// value 저장 값
		String value = String.format("{\"matchId\":\"%s\",\"attackerId\":\"%s\", \"hitId\":\"%s\" ,\"damage\":%d, \"weapon\":\"%s\", \"ts\":%d}",        
				req.getMatchId(), req.getAttackerId(), req.getHitId(), req.getDamage(), req.getWeapon(), timeStamp
				);
		
		// Redis List로 같은 Key 값이어도 겹치지 않게 한다.
		// 요청에 TTL값이 0 존재한다면 주어진 값을, 
		// 그렇지 않다면 30초동안 Redis에 Key-Value를 보관한다.
		redis.opsForList().leftPush(key, value);
		
		long ttl = (req.getTtlSeconds()!= null && req.getTtlSeconds() > 0) ? req.getTtlSeconds() :30L;
		redis.expire(key, Duration.ofSeconds(ttl));
		
	}
	
	/**
	 * Log에 저장된 게임 데이터에 대한 분석된 정보 가져오기
	 * @param
	 * @return
	 **/
	public void getGameInfo() {	
		
		
		
	}
	
	
}
