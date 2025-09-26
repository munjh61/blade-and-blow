package org.ssafy.gamedataserver.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.ssafy.gamedataserver.entity.battle.Mode;
import org.ssafy.gamedataserver.entity.battle.Stat;

import java.util.List;
import java.util.Optional;

public interface StatRepository extends JpaRepository<Stat,Long> {
    Optional<List<Stat>> findAllByUserIdAndMode(long userId, Mode mode);
    boolean existsByUserId(long userId);
}
