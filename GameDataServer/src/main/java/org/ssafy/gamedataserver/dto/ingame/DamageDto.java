package org.ssafy.gamedataserver.dto.ingame;

import jakarta.validation.constraints.NotBlank;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import lombok.ToString;


@Getter
@Setter
@ToString
@AllArgsConstructor
@NoArgsConstructor
public class DamageDto {
	@NotBlank
	private String matchId;
	@NotBlank
	private String attackerId;
	@NotBlank
	private String hitId;
	
	private int damage;
	
	private String weapon;
	
	private Long ttlSeconds;
	
	private Long timeStamp;

}
