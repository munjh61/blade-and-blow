package org.ssafy.gamedataserver.security;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.SignatureAlgorithm;
import io.jsonwebtoken.security.Keys;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.nio.charset.StandardCharsets;
import java.security.Key;
import java.time.Duration;
import java.time.Instant;
import java.util.Date;

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
        if (secret == null) throw new IllegalStateException("jwt.secret is null");
        if (secret.getBytes(StandardCharsets.UTF_8).length < 32)
            throw new IllegalStateException("jwt.secret must be at least 32 bytes");
        this.key = Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8));
        this.accessMinutes = accessMinutes;
        this.refreshDays = refreshDays;
    }

    // 토큰 생성
    public String generateToken(String username, TokenType tokenType, long ver) {
        Instant now = Instant.now();
        Duration life = (tokenType == TokenType.ACCESS)
                ? Duration.ofMinutes(accessMinutes)
                : Duration.ofDays(refreshDays);
        Date expiry = Date.from(now.plus(life));
        return Jwts.builder()
                .setSubject(username)
                .setIssuedAt(Date.from(now))
                .setExpiration(expiry)
                .claim(CLAIM_TOKEN_TYPE, tokenType)
                .claim("ver", ver)
                .signWith(key, SignatureAlgorithm.HS256)
                .compact();
    }

    // 토큰에서 사용자명 추출
    public String getUsername(String token) {
        return Jwts.parserBuilder()
                .setSigningKey(key)
                .build()
                .parseClaimsJws(token).getBody().getSubject();
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
    public Claims getClaims(String token) {
        return Jwts.parserBuilder().
                setSigningKey(key).
                build()
                .parseClaimsJws(token).getBody();
    }

    // 리프레시 토큰
    public boolean isRefreshToken(String token) {
        try {
            TokenType t = getClaims(token).get(CLAIM_TOKEN_TYPE, TokenType.class);
            return TokenType.REFRESH.equals(t);
        } catch (JwtException | IllegalArgumentException e) {
            return false;
        }
    }

    // 버전
    public long getVersion(String token) {
        return getClaims(token).get("ver", Long.class);
    }
}
