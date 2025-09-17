package org.ssafy.gamedataserver.config;

import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.dao.DaoAuthenticationProvider;
import org.springframework.security.config.annotation.authentication.configuration.AuthenticationConfiguration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;
import org.springframework.web.cors.CorsConfiguration;
import org.springframework.web.cors.CorsConfigurationSource;
import org.springframework.web.cors.UrlBasedCorsConfigurationSource;
import org.ssafy.gamedataserver.security.CustomUserDetailService;
import org.ssafy.gamedataserver.security.JwtAuthenticationFilter;

import java.util.List;

@Configuration
@RequiredArgsConstructor
public class SecurityConfig {
    private final CustomUserDetailService customUserDetailService;
     private final JwtAuthenticationFilter jwtAuthenticationFilter;

    @Bean
    public BCryptPasswordEncoder bCryptPasswordEncoder() {
        return new BCryptPasswordEncoder();
    }
    @Bean
    public AuthenticationProvider authenticationProvider() {
        DaoAuthenticationProvider provider = new DaoAuthenticationProvider();
        provider.setUserDetailsService(customUserDetailService);
        provider.setPasswordEncoder(bCryptPasswordEncoder());
        return provider;
    }

    @Bean
    public AuthenticationManager authenticationManager(AuthenticationConfiguration authenticationConfiguration) throws Exception {
        return authenticationConfiguration.getAuthenticationManager();
    }

    @Bean
    public CorsConfigurationSource ccfs(){
        // 어떤 config인지 설정
        CorsConfiguration ccf = new CorsConfiguration();
        ccf.setAllowedOrigins(List.of(
//                "http://localhost:5173","https://localhost:5173",
                "http://j13a405.p.ssafy.io","https://j13a405.p.ssafy.io",
                "http://3.36.183.255","https://3.36.183.255"
        ));
        ccf.setAllowedMethods(List.of("GET","POST","PUT","PATCH","DELETE","OPTIONS"));
        ccf.addAllowedHeader("*");
        ccf.setAllowCredentials(true);
        // 위에서 정의한 config를 어디에 적용할지 설정
        UrlBasedCorsConfigurationSource ubccfs = new UrlBasedCorsConfigurationSource();
        ubccfs.registerCorsConfiguration("/**",ccf);

        return ubccfs;
    }

    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        http
                .csrf(csrf -> csrf.disable()) // 시큐리티 기본 로그인 기능 끄기
                .cors(cors -> cors.configurationSource(ccfs())) // cors
                .sessionManagement(sessionManagement -> sessionManagement.sessionCreationPolicy(SessionCreationPolicy.STATELESS)) // JSESSIONID 생성 못하도록
                .authorizeHttpRequests(auth -> auth // URL 인가 규칙
                        .requestMatchers("/api/v1/auth/**").permitAll() // 로그인 관련 인증 필요 X
                        .requestMatchers("/api/v1/public/**").permitAll()
                        .anyRequest().authenticated()
                )
                .authenticationProvider(authenticationProvider());
        http.addFilterBefore(jwtAuthenticationFilter, UsernamePasswordAuthenticationFilter.class);
        return http.build();
    }
}
