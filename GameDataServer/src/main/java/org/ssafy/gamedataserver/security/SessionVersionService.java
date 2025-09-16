package org.ssafy.gamedataserver.security;

import org.springframework.stereotype.Service;

import java.util.concurrent.ConcurrentHashMap;

// 여러 디바이스에서 동시 접속을 막기 위한 서비스이다
@Service
public class SessionVersionService {
    private final ConcurrentHashMap<String, Long> userVersions = new ConcurrentHashMap<>();

    public long getUserVersion(String username) {
        return userVersions.getOrDefault(username, 0L);
    }

    public long setUserVersion(String username) {
        // 동시성 문제로 get 한 다음 set 하면 안되고 merge 사용해야한다.
        return userVersions.merge(username, 1L, Long::sum);
    }
}
