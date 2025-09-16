//package org.ssafy.gamedataserver.entity.battle;
//
//import jakarta.persistence.*;
//import lombok.*;
//import org.hibernate.annotations.CreationTimestamp;
//import org.hibernate.annotations.UpdateTimestamp;
//import org.ssafy.gamedataserver.entity.user.User;
//
//import java.time.LocalDateTime;
//
//@Entity
//@Table(name = "participants")
//@Getter
//@Setter
//@Builder
//@NoArgsConstructor
//@AllArgsConstructor
//public class Participant {
//    @Id
//    @GeneratedValue(strategy = GenerationType.IDENTITY)
//    private Long id;
//
//    @ManyToOne(fetch = FetchType.LAZY)
//    @JoinColumn(nullable = false)
//    private Game game;
//
//    @CreationTimestamp
//    LocalDateTime joinedAt;
//
//    @ManyToOne(fetch = FetchType.LAZY)
//    @JoinColumn(nullable = false)
//    User user; // 로그인한 사람
//
//    @ManyToOne(fetch = FetchType.LAZY)
//    @JoinColumn(name = "killed_by_who", nullable = false)
//    User killedByWho;
//
//    @Column
//    String killedByWhat;
//
//    @UpdateTimestamp
//    LocalDateTime killedTime;
//
//}
