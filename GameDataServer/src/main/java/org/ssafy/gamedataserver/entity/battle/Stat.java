package org.ssafy.gamedataserver.entity.battle;

import jakarta.persistence.*;
import lombok.*;
import org.ssafy.gamedataserver.entity.user.User;

@Entity
@Table(name = "stats")
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class Stat {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn
    private User user;
    @Column
    private long win;
    @Column
    private long lose;
    @Column
    private long kill;
    @Column
    private long death;
    @Column
    private long damage;
    @Enumerated(EnumType.STRING)
    @Column
    private Mode mode;
    @Enumerated(EnumType.STRING)
    @Column
    private Weapon weapon;
}
