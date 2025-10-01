package org.ssafy.gamedataserver.config;

import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableScheduling;

// Application Background에서 Scheduling이 가능하게 함.
@Configuration
@EnableScheduling
public class SchedulingConfig {
}
