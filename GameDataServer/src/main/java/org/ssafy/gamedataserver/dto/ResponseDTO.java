package org.ssafy.gamedataserver.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.Builder;
import lombok.Getter;
import org.springframework.http.HttpStatus;

@Getter
@JsonInclude(JsonInclude.Include.NON_NULL)
public class ResponseDTO<T> {
    private final int status;
    private final String message;
    private final T data;
    private final long timestamp;

    @Builder
    private ResponseDTO(HttpStatus status, String message, T data, long timestamp) {
        this.status = status.value();
        this.message = message;
        this.data = data;
        this.timestamp = timestamp;
    }

    public static <T> ResponseDTO<T> ok(T data) {
        return ResponseDTO.<T>builder()
                .status(HttpStatus.OK)
                .data(data)
                .timestamp(System.currentTimeMillis())
                .build();
    }

    public static <T> ResponseDTO<T> ok(String message, T data) {
        return ResponseDTO.<T>builder()
                .status(HttpStatus.OK)
                .message(message)
                .data(data)
                .timestamp(System.currentTimeMillis())
                .build();
    }

    public static <T> ResponseDTO<T> fail(String message, HttpStatus status) {
        return ResponseDTO.<T>builder()
                .status(status)
                .message(message)
                .timestamp(System.currentTimeMillis())
                .build();
    }
}
