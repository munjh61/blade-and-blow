package org.ssafy.gamedataserver.dto.user;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class LoginDTO {
    private long userId;
    private String nickname;
    private String accessToken;
    private String refreshToken;
}
