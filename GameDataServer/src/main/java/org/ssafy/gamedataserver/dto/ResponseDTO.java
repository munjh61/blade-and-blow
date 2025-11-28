package org.ssafy.gamedataserver.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.Builder;
import lombok.Getter;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

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

    /* ========= Core builders ========= */
    public static <T> ResponseDTO<T> of(HttpStatus status, String message, T data) {
        return ResponseDTO.<T>builder()
                .status(status)
                .message(message)
                .data(data)
                .timestamp(System.currentTimeMillis())
                .build();
    }
    public static <T> ResponseDTO<T> of(HttpStatus status, String message) {
        return of(status, message, null);
    }

    /* ========= DTO only (기존 유지) ========= */
    public static <T> ResponseDTO<T> okDTO(T data) { return of(HttpStatus.OK, null, data); }
    public static <T> ResponseDTO<T> okDTO(String message, T data) { return of(HttpStatus.OK, message, data); }
    public static <T> ResponseDTO<T> failDTO(String message, HttpStatus status) { return of(status, message); }

    /* ========= Entity helpers (바로 ResponseEntity 반환) ========= */
    public static <T> ResponseEntity<ResponseDTO<T>> ok(T data) {
        return ResponseEntity.ok(okDTO(data));
    }
    public static <T> ResponseEntity<ResponseDTO<T>> ok(String message, T data) {
        return ResponseEntity.ok(okDTO(message, data));
    }
    public static <T> ResponseEntity<ResponseDTO<T>> created(String message, T data) {
        return ResponseEntity.status(HttpStatus.CREATED).body(of(HttpStatus.CREATED, message, data));
    }
    public static <T> ResponseEntity<ResponseDTO<T>> badRequest(String message) {
        return ResponseEntity.badRequest().body(of(HttpStatus.BAD_REQUEST, message));
    }
    public static <T> ResponseEntity<ResponseDTO<T>> unauthorized(String message) {
        return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(of(HttpStatus.UNAUTHORIZED, message));
    }
    public static <T> ResponseEntity<ResponseDTO<T>> forbidden(String message) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN).body(of(HttpStatus.FORBIDDEN, message));
    }
    public static <T> ResponseEntity<ResponseDTO<T>> notFound(String message) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(of(HttpStatus.NOT_FOUND, message));
    }
    public static <T> ResponseEntity<ResponseDTO<T>> conflict(String message) {
        return ResponseEntity.status(HttpStatus.CONFLICT).body(of(HttpStatus.CONFLICT, message));
    }

}
