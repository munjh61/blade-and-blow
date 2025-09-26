package org.ssafy.gamedataserver.dto.stat;

import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
@Builder
public class WeaponDTO {
    private long win;
    private long lose;
    private long kill;
    private long death;
    private long damage;
}
