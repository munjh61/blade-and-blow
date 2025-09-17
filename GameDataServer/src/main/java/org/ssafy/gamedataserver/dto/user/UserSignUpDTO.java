package org.ssafy.gamedataserver.dto.user;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class UserSignUpDTO {
    String username;
    String password;
    String nickname;
}
