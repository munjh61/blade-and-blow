pipeline {                                // 파이프라인 선언 시작
  agent any                                // 어떤 에이전트(노드)에서나 실행 (여기선 jenkins 컨테이너)

  options {
    timestamps()                           // 콘솔 로그에 타임스탬프 표시
    skipDefaultCheckout(true)              // Jenkins 기본 체크아웃(Stage 중복 방지)
  }

  // 고정값/경로: compose 바이너리/파일 경로 지정
  environment {
    COMPOSE_BIN  = '/usr/local/bin/docker-compose'   // compose v2 단일 바이너리 설치 경로
    COMPOSE_FILE = 'docker-compose.yml'              // 레포 루트의 compose 파일
  }

  stages {

    stage('Checkout') {                    // 1) 소스 체크아웃 단계
      steps {
        checkout scm                       // “Pipeline script from SCM”로 설정된 GitLab에서 코드 가져오기
      }
    }

    stage('Build JAR') {                   // 2) 스프링 부트 JAR 빌드 단계
      steps {
        dir('GameDataServer') {            // Dockerfile과 gradlew가 있는 폴더로 이동
          sh 'chmod +x gradlew || true'    // gradlew 실행권한 부여(이미 있으면 무시)
          sh './gradlew clean bootJar'     // 부트 JAR 생성(우리가 build.gradle에서 app.jar로 고정)
        }
      }
    }

    stage('Build & Deploy (docker compose)') { // 3) 컨테이너 빌드/배포 단계
      steps {
        // Jenkins Credentials → Secret file(ID: stack-env)로 올려둔 .env를 ENVFILE 경로로 제공
        withCredentials([file(credentialsId: 'stack-env', variable: 'ENVFILE')]) {
          // .env를 워크스페이스에 복사하지 않고, --env-file로 직접 읽게 함(권장)
          // --build: 이미지 재빌드, --force-recreate: 환경 변경 반영 위해 컨테이너 재생성
          sh '${COMPOSE_BIN} --env-file "$ENVFILE" -f ${COMPOSE_FILE} up -d --build --force-recreate'
        }
      }
    }

    stage('Verify') {                      // 4) 기본 검증 단계
      steps {
        withCredentials([file(credentialsId: 'stack-env', variable: 'ENVFILE')]) {
          sh '${COMPOSE_BIN} --env-file "$ENVFILE" -f ${COMPOSE_FILE} ps'                 // 실행 중인 서비스 확인
          sh '${COMPOSE_BIN} --env-file "$ENVFILE" -f ${COMPOSE_FILE} logs --no-color mysql | tail -n 80 || true' // MySQL 최근 로그
          sh '${COMPOSE_BIN} --env-file "$ENVFILE" -f ${COMPOSE_FILE} logs --no-color api   | tail -n 120 || true' // API 최근 로그
        }
      }
    }
  }

  // 실패 시 추가 디버깅 정보 출력
  post {
    failure {
      sh 'docker ps --format "table {{.Names}}\\t{{.Image}}\\t{{.Status}}\\t{{.Ports}}" || true'
    }
  }
}
