package org.ssafy.gamedataserver.dto.battle;

import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
@Builder
public class WeaponDTO {
    private long wins;
    private long losses;
    private long kills;
    private long deaths;
    private long damage;
}
