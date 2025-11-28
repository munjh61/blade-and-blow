package org.ssafy.gamedataserver.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.ssafy.gamedataserver.entity.user.User;

import java.util.Optional;

public interface UserRepository extends JpaRepository<User, Long> {
    Optional<User> findByUsername(String username);
    boolean existsByUsername(String username);
}
