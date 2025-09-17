//package org.ssafy.gamedataserver.entity.battle;
//
//import jakarta.persistence.*;
//import lombok.*;
//import org.ssafy.gamedataserver.entity.user.User;
//
//@Entity
//@Table(name = "records")
//@Getter
//@Setter
//@Builder
//@NoArgsConstructor
//@AllArgsConstructor
//public class Record {
//    @Id
//    @GeneratedValue(strategy = GenerationType.IDENTITY)
//    private Long id;
//    @ManyToOne(fetch = FetchType.LAZY)
//    @JoinColumn
//    private User user;
//    @Column
//    private int wins;
//    @Column
//    private int losses;
//    @Column
//    private long kills;
//    @Column
//    private Mode mode;
//    @Column
//    private String weapon;
//}
