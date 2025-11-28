package org.ssafy.gamedataserver.service.ingame;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;

import org.bson.Document;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.redis.connection.RedisConnection;
import org.springframework.data.redis.core.Cursor;
import org.springframework.data.redis.core.ScanOptions;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.mongodb.client.MongoCollection;

@Service
public class FlushService {

  private static final String PREFIX = "game:prod:kill:"; // game:prod:hit:{matchId}:{attackerId}
  private final StringRedisTemplate redis;
  private final MongoTemplate mongo;
  private final ObjectMapper om = new ObjectMapper();

  public FlushService(StringRedisTemplate redis, MongoTemplate mongo) {
    this.redis = redis; this.mongo = mongo;
  }
  
  @Scheduled(fixedDelay = 10_000)
  public void flush() {
	  // Redis와의 통신을 위한 StringRedisTemplate 객체 이용
	  // 모든 game:prod:hit: => prefix 가져오기
	  redis.execute((RedisConnection conn) -> {
          final int GLOBAL_BATCH_SIZE = 100;
          final int PER_KEY_POP_SIZE = 50;
          
		  ScanOptions options = ScanOptions.scanOptions()
                  .match(PREFIX + "*")
                  .count(10000)
                  .build();
          
          int scanned =0, inserted = 0, skipped = 0, deleted = 0;
          
          try (Cursor<byte[]> cur = conn.scan(options)) {
        	  	 System.out.println("Scanning");
        	  	 
        	  	 // Batch 단위로 데이터를 넣기 위한 객체 형성
                 List<org.bson.Document> globalBatch = new ArrayList<>();
                 
        	  	 // Redis의 각 엔트리에서, key의 value(JSON)추출.
        	  	 while (cur.hasNext()) {
	            	  scanned ++;
	            	  
	                  String key = new String(cur.next(), StandardCharsets.UTF_8);
	                  var type = redis.type(key);
	                  if (type == null || !"list".equalsIgnoreCase(type.code())) {
	                      skipped++;
	                      
	                      System.out.println("Redis Key is not a List Type!");
	                      continue;
	                    }
	                  
	                  // 동일한 Key에 대해서 LeftPush 한 것을 Right Pop으로
	                  // Queue에 넣은 순서대로 뺸다.
	                  while(true) {
	                	  String json = redis.opsForList().rightPop(key);
	                	  if (json == null) break;
	                		
				          JsonNode node = om.readTree(json);
				          
				          DateTimeFormatter ISO_FORMAT = DateTimeFormatter.ISO_INSTANT;
				          Long epochMillis = node.has("ts")? node.get("ts").asLong() : null;
				          epochMillis = (epochMillis != null && epochMillis > 0 ? epochMillis : Instant.now().toEpochMilli()) ;
				          String isoTs = ISO_FORMAT.format(Instant.ofEpochMilli(epochMillis));
				          
				          var doc = new org.bson.Document()
				            .append("matchId",     node.get("matchId").asText(null))
				            .append("attackerId",  node.get("attackerId").asText(null))
				            .append("hitId",       node.get("hitId").asText(null))
				            .append("damage",      node.get("damage").asInt(0))
				            .append("weapon",      node.get("weapon").asText(null))
				            .append("ts",          isoTs);
				          
				          // 다음 키로 넘어가서 globalBatch에 document를 더한다.
				          globalBatch.add(doc);
				          
				          // globalBatch의 크기를 넘어가면 한꺼번에 넣는다.
				          if(globalBatch.size() >=  GLOBAL_BATCH_SIZE) {
				              mongo.insert(globalBatch, "kill_events");
				              inserted += globalBatch.size();
				        	  globalBatch.clear();
				          }
				          // mongo.insert(doc, "kill_events");   // ✅ append-only 저장
				          // inserted ++;
	                  }
	                  
	                  // GlobalBatch내 잔여 데이터를 넣는다.
	                  if(!globalBatch.isEmpty()) {
	                	  mongo.insert(globalBatch, "kill_events");
	                	  // 드라이버는 _id를 제자리(mutating)로 채워넣습니다. 실제로 들어간 _id를 바로 확인 가능.
	                	  // globalBatch.forEach(d -> System.out.println("[MONGO] inserted _id=" + d.getObjectId("_id")));
	                	  inserted += globalBatch.size();
	                	  
	                	  globalBatch.clear();
	                	  // System.out.println("[MONGO] DB=" + mongo.getDb().getName());
	             	  	  // System.out.println("[MONGO] Collections=" + mongo.getDb().listCollectionNames().into(new java.util.ArrayList<>()));
	                	  
	             	  	  System.out.println("Total Global Batch is inserted and cleared");
	                  }else {
	                	  System.out.println("Global Batch is Empty");
	                  }
	                  
        	  	 }
        	  	 
        	  	 if(scanned!=0) {              	  
        	  		System.out.printf("%d keys Scanned, %d values detected\n", scanned, inserted);
           	  	 	System.out.println("All data inserted to MongoDB");
           	  	 	
           	  	 	MongoCollection<Document> coll = mongo.getCollection("kill_events");
           	  	 	System.out.println("[MONGO] count now = " + coll.countDocuments());
        	  	 }
        	  	 
     
      } catch (Exception e) { 
    	  System.out.println("Failed to scan Redis key");
      }
      return null;
    });
  }
}
