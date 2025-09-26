package org.ssafy.gamedataserver.service;

import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Service;
import org.ssafy.gamedataserver.dto.stat.StatModeDTO;
import org.ssafy.gamedataserver.dto.stat.StatWeaponDTO;
import org.ssafy.gamedataserver.entity.battle.Mode;
import org.ssafy.gamedataserver.entity.battle.Stat;
import org.ssafy.gamedataserver.entity.battle.Weapon;
import org.ssafy.gamedataserver.entity.user.User;
import org.ssafy.gamedataserver.repository.StatRepository;
import org.ssafy.gamedataserver.repository.UserRepository;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
@RequiredArgsConstructor
public class StatService {
    private final UserRepository userRepository;
    private final StatRepository statRepository;

    @Transactional
    public void init() {
        Optional<User> user = getCurrentUser();
        if (user.isPresent() && statRepository.existsByUserId(user.get().getId())) {
            List<Stat> batch = new ArrayList<>(9);
            for (Mode mode : Mode.values()) {
                for (Weapon weapon : Weapon.values()) {
                    Stat r = Stat.builder()
                            .user(user.get())
                            .mode(mode)
                            .weapon(weapon)
                            .build();
                    batch.add(r);
                }
            }
            statRepository.saveAll(batch);
        }
    }

    @Transactional
    public StatModeDTO getStat(Mode mode) {
        StatModeDTO statModeDTO = new StatModeDTO();
        User user = getCurrentUser().get();
        Optional<List<Stat>> op = statRepository.findAllByUserIdAndMode(user.getId(), mode);
        if (op.isPresent()) {
            List<Stat> stats = op.get();
            for (Stat stat : stats) {
                StatWeaponDTO dto = StatWeaponDTO
                        .builder()
                        .win(stat.getWin())
                        .lose(stat.getLose())
                        .kill(stat.getKill())
                        .death(stat.getDeath())
                        .damage(stat.getDamage())
                        .build();
                switch (stat.getWeapon()) {
                    case SWORD -> statModeDTO.setSword(dto);
                    case BOW -> statModeDTO.setBow(dto);
                    case WAND -> statModeDTO.setWand(dto);
                }
            }
        } else {
            init();
        }
        return statModeDTO;
    }

    // 조회
    private Optional<User> getCurrentUser() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        String username = authentication.getName();
        return userRepository.findByUsername(username);
    }
}
