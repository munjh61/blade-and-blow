package org.ssafy.gamedataserver.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.ssafy.gamedataserver.entity.battle.Mode;
import org.ssafy.gamedataserver.entity.battle.BattleStat;

import java.util.List;
import java.util.Optional;

public interface BattleStatRepository extends JpaRepository<BattleStat,Long> {
    Optional<List<BattleStat>> findAllByUserIdAndMode(long userId, Mode mode);
    boolean existsByUserId(long userId);
}
