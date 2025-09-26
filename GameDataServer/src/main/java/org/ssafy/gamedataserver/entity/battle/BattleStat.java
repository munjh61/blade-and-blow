package org.ssafy.gamedataserver.entity.battle;

import jakarta.persistence.*;
import lombok.*;
import org.ssafy.gamedataserver.entity.user.User;

@Entity(name = "BattleStat")
@Table(name = "battle_stats")
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class BattleStat {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn
    private User user;
    @Column
    private long wins;
    @Column
    private long losses;
    @Column
    private long kills;
    @Column
    private long deaths;
    @Column
    private long damage;
    @Enumerated(EnumType.STRING)
    @Column
    private Mode mode;
    @Enumerated(EnumType.STRING)
    @Column
    private Weapon weapon;
}
