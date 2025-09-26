package org.ssafy.gamedataserver.dto.battle;

import lombok.Getter;
import lombok.Setter;
import org.ssafy.gamedataserver.entity.battle.Mode;

@Getter
@Setter
public class BattleStatModeDTO {
    private Mode mode;
    private WeaponDTO sword;
    private WeaponDTO bow;
    private WeaponDTO wand;
}
