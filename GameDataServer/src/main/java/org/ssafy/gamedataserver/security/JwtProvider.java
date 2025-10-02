package org.ssafy.gamedataserver.security;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.SignatureAlgorithm;
import io.jsonwebtoken.security.Keys;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import org.ssafy.gamedataserver.entity.user.Role;

import java.nio.charset.StandardCharsets;
import java.security.Key;
import java.time.Duration;
import java.time.Instant;
import java.util.Date;
import java.util.List;
import java.util.Set;

@Component
public class JwtProvider {
    public enum TokenType {ACCESS, REFRESH}

    private static final String CLAIM_TOKEN_TYPE = "token_type";

    private final Key key;
    private final long accessMinutes;
    private final long refreshDays;

    public JwtProvider(
            @Value("${jwt.secret}") String secret,
            @Value("${jwt.access-token-minutes}") long accessMinutes,
            @Value("${jwt.refresh-token-days}") long refreshDays
    ) {
        this.key = Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8));
        this.accessMinutes = accessMinutes;
        this.refreshDays = refreshDays;
    }

    // 토큰 생성
    public String generateToken(
            Long id,
            String username,
            Set<Role> roles,
            TokenType tokenType,
            long ver,
            String mac,
            long deviceVer
    ) {
        Instant now = Instant.now();
        Duration life = (tokenType == TokenType.ACCESS)
                ? Duration.ofMinutes(accessMinutes)
                : Duration.ofDays(refreshDays);
        Date expiry = Date.from(now.plus(life));
        List<String> roleNames = roles.stream().map(Enum::name).toList();
        return Jwts.builder()
                .setSubject(id.toString())
                .setIssuedAt(Date.from(now))
                .setExpiration(expiry)
                .claim("roles", roleNames)
                .claim(CLAIM_TOKEN_TYPE, tokenType)
                .claim("username", username)
                .claim("ver", ver)
                .claim("mac", mac)
                .claim("deviceVer", deviceVer)
                .signWith(key, SignatureAlgorithm.HS256)
                .compact();
    }

    // 토큰 유효성 검사
    public boolean isTokenValid(String token) {
        try {
            Jwts.parserBuilder()
                    .setSigningKey(key)
                    .build()
                    .parseClaimsJws(token);
            return true;
        } catch (JwtException | IllegalArgumentException e) {
            return false;
        }
    }

    // 클레임 확인
    private Claims getClaims(String token) {
        return Jwts.parserBuilder().
                setSigningKey(key).
                build()
                .parseClaimsJws(token).getBody();
    }

    // 아이디
    public String getUsername(String token) {
        return getClaims(token).get("username", String.class);
    }

    // 아이디 pk
    public Long getUserId(String token) {
        return Long.parseLong(getClaims(token).getSubject());
    }

    public List<String> getRoles(String token) {
        return getClaims(token).get("roles", List.class);
    }

    // 리프레시 토큰
    public boolean isRefreshToken(String token) {
        try {
            String type = getClaims(token).get(CLAIM_TOKEN_TYPE, String.class);
            return "REFRESH".equals(type);
        } catch (JwtException | IllegalArgumentException e) {
            return false;
        }
    }

    // 버전
    public long getVersion(String token) {
        return getClaims(token).get("ver", Long.class);
    }

    public String getMac(String token) {
        return getClaims(token).get("mac", String.class);
    }

    public long getDeviceVersion(String token) {
        return getClaims(token).get("deviceVer", Long.class);
    }
}
