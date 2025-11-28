package org.ssafy.gamedataserver.config;

import java.util.List;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.config.annotation.authentication.configuration.AuthenticationConfiguration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.web.cors.CorsConfiguration;
import org.springframework.web.cors.CorsConfigurationSource;
import org.springframework.web.cors.UrlBasedCorsConfigurationSource;

@Configuration
@Profile("dev")	
public class DevSecurityConfig {
	
	@Bean
    public BCryptPasswordEncoder bCryptPasswordEncoder() {
        return new BCryptPasswordEncoder();
    }

    // AuthenticationManager가 필요하다면 (AuthController에서 주입받음)
    @Bean
    public AuthenticationManager authenticationManager(AuthenticationConfiguration cfg) throws Exception {
        return cfg.getAuthenticationManager();
    }

    // CORS 전역 설정
    @Bean
    public CorsConfigurationSource ccfs() {
        var ccf = new CorsConfiguration();
        ccf.setAllowedOriginPatterns(List.of("http://localhost:*", "http://127.0.0.1:*",
                                             "http://j13a405.p.ssafy.io","https://j13a405.p.ssafy.io",
                                             "http://3.36.183.255","https://3.36.183.255"));
        ccf.setAllowedMethods(List.of("GET","POST","PUT","PATCH","DELETE","OPTIONS"));
        ccf.addAllowedHeader("*");
        ccf.setAllowCredentials(true);

        var src = new UrlBasedCorsConfigurationSource();
        src.registerCorsConfiguration("/**", ccf);
        return src;
    }
    
	@Bean
	public SecurityFilterChain devSecurityFilterChain(HttpSecurity http, CorsConfigurationSource ccfs) throws Exception{
		
		http
			.csrf(csrf->csrf.disable())
			.cors(cors -> cors.configurationSource(ccfs))
	        .sessionManagement(s -> s.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
	        .authorizeHttpRequests(auth -> auth.anyRequest().permitAll());
		return http.build();
	}
	
}
