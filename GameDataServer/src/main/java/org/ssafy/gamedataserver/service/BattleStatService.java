package org.ssafy.gamedataserver.service;

import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Service;
import org.ssafy.gamedataserver.dto.battle.BattleStatModeDTO;
import org.ssafy.gamedataserver.dto.battle.WeaponDTO;
import org.ssafy.gamedataserver.entity.battle.Mode;
import org.ssafy.gamedataserver.entity.battle.BattleStat;
import org.ssafy.gamedataserver.entity.battle.Weapon;
import org.ssafy.gamedataserver.entity.user.User;
import org.ssafy.gamedataserver.repository.BattleStatRepository;
import org.ssafy.gamedataserver.repository.UserRepository;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
@RequiredArgsConstructor
public class BattleStatService {
    private final UserRepository userRepository;
    private final BattleStatRepository battleStatRepository;

    @Transactional
    public void init() {
        Optional<User> user = getCurrentUser();
        if (user.isPresent() && battleStatRepository.existsByUserId(user.get().getId())) {
            List<BattleStat> batch = new ArrayList<>(9);
            for (Mode mode : Mode.values()) {
                for (Weapon weapon : Weapon.values()) {
                    BattleStat r = BattleStat.builder()
                            .user(user.get())
                            .mode(mode)
                            .weapon(weapon)
                            .build();
                    batch.add(r);
                }
            }
            battleStatRepository.saveAll(batch);
        }
    }

    @Transactional
    public BattleStatModeDTO getBattleStat(Mode mode) {
        BattleStatModeDTO battleStatModeDTO = new BattleStatModeDTO();
        User user = getCurrentUser().get();
        Optional<List<BattleStat>> op = battleStatRepository.findAllByUserIdAndMode(user.getId(), mode);
        if (op.isPresent()) {
            List<BattleStat> battleStats = op.get();
            for (BattleStat battleStat : battleStats) {
                WeaponDTO dto = WeaponDTO
                        .builder()
                        .wins(battleStat.getWins())
                        .losses(battleStat.getLosses())
                        .kills(battleStat.getKills())
                        .deaths(battleStat.getDeaths())
                        .damage(battleStat.getDamage())
                        .build();
                switch (battleStat.getWeapon()) {
                    case SWORD -> battleStatModeDTO.setSword(dto);
                    case BOW -> battleStatModeDTO.setBow(dto);
                    case WAND -> battleStatModeDTO.setWand(dto);
                }
            }
        } else {
            init();
        }
        return battleStatModeDTO;
    }

    // 조회
    private Optional<User> getCurrentUser() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        String username = authentication.getName();
        return userRepository.findByUsername(username);
    }
}
