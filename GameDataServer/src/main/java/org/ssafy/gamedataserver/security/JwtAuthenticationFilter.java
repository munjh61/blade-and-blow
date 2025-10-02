package org.ssafy.gamedataserver.security;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;
import org.ssafy.gamedataserver.entity.user.Role;
import org.ssafy.gamedataserver.entity.user.User;

import java.io.IOException;
import java.util.Set;
import java.util.stream.Collectors;

@Component
@RequiredArgsConstructor
public class JwtAuthenticationFilter extends OncePerRequestFilter {
    private final JwtProvider jwtProvider;
    private final SessionVersionService sessionVersionService;

    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain) throws ServletException, IOException {
        String authHeader = request.getHeader("Authorization");
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (jwtProvider.isTokenValid(token) && !jwtProvider.isRefreshToken(token)) {
                long id = jwtProvider.getUserId(token);
                long tokenVer = jwtProvider.getVersion(token);
                long serverVer = sessionVersionService.getUserVersion(id);
                if (tokenVer == serverVer) {
                    String username = jwtProvider.getUsername(token);
                    Set<Role> roleSet = jwtProvider.getRoles(token).stream().map(Role::valueOf).collect(Collectors.toSet());
                    User user = User.builder()
                            .id(id)
                            .roles(roleSet)
                            .username(username)
                            .build();
                    CustomUserDetails userDetails = CustomUserDetails.from(user);
                    UsernamePasswordAuthenticationToken authentication = new UsernamePasswordAuthenticationToken(userDetails, null, userDetails.getAuthorities());
                    authentication.setDetails(authentication.getDetails());
                    SecurityContextHolder.getContext().setAuthentication(authentication);
                }
            }
        }
        chain.doFilter(request, response);
    }
}
