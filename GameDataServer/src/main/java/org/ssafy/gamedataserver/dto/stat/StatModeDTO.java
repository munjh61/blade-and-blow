package org.ssafy.gamedataserver.dto.stat;

import lombok.Getter;
import lombok.Setter;
import org.ssafy.gamedataserver.entity.battle.Mode;

@Getter
@Setter
public class StatModeDTO {
    private Mode mode;
    private StatWeaponDTO sword;
    private StatWeaponDTO bow;
    private StatWeaponDTO wand;
}
