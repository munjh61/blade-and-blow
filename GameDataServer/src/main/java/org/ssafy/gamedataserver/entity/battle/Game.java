//package org.ssafy.gamedataserver.entity.battle;
//
//import jakarta.persistence.*;
//import lombok.*;
//import org.hibernate.annotations.CreationTimestamp;
//import org.hibernate.annotations.UpdateTimestamp;
//import org.ssafy.gamedataserver.entity.user.User;
//
//import java.time.LocalDateTime;
//import java.util.UUID;
//
//@Entity
//@Table(name = "games")
//@Getter
//@Setter
//@Builder
//@NoArgsConstructor
//@AllArgsConstructor
//public class Game {
//    @Id
//    private String id = UUID.randomUUID().toString();
//    @Column(nullable = false)
//    String roomName;
//    @Enumerated(EnumType.STRING)
//    @Column(nullable = false)
//    Mode mode;
//    @ManyToOne(fetch = FetchType.LAZY)
//    @JoinColumn(name = "winner_user_id")
//    private User winner;
//    @CreationTimestamp
//    LocalDateTime startTime;
//    @UpdateTimestamp
//    LocalDateTime endTime;
//}
