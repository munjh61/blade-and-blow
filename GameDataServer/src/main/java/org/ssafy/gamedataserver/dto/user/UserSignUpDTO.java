package org.ssafy.gamedataserver.dto.user;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class UserSignUpDTO {
    private String username;
    private String password;
    private String nickname;
}
