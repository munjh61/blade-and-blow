package org.ssafy.gamedataserver.security;

import org.springframework.stereotype.Service;

import java.util.concurrent.ConcurrentHashMap;

// 여러 디바이스에서 동시 접속을 막기 위한 서비스이다
@Service
public class SessionVersionService {
    private final ConcurrentHashMap<Long, Long> userVersions = new ConcurrentHashMap<>();

    public long getUserVersion(Long id) {
        return userVersions.getOrDefault(id, 0L);
    }

    public long setUserVersion(Long id) {
        // 동시성 문제로 get, set 하면 안되고 merge 함수를 사용함.
        return userVersions.merge(id, 1L, Long::sum);
    }
}
