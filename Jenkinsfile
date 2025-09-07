pipeline {                                // 파이프라인 선언 시작
  agent any                                // 어떤 에이전트(노드)에서나 실행 (여기서는 jenkins 컨테이너)

  options { timestamps() }                 // 콘솔 로그에 타임스탬프 표시(문제 추적에 유용)

  stages {                                 // 단계(Stages) 모음

    stage('Checkout') {                    // 1) 소스 체크아웃 단계
      steps { 
        checkout scm                       // Job에 설정된 SCM(여기선 GitLab)에서 코드 가져오기
      }
    }

    stage('Provide .env (from Jenkins)') { // 2) .env 시크릿 주입 단계
      steps {
        // Jenkins Credentials에 등록해 둔 "Secret file"(.env)을 워크스페이스로 복사
        // - credentialsId: Jenkins에 저장한 ID(예: stack-env)
        // - variable: 임시 환경변수명(여기에 시크릿 파일 경로가 들어옴)
        withCredentials([file(credentialsId: 'stack-env', variable: 'ENVFILE')]) {
          sh 'cp "$ENVFILE" .env'          // 레포 루트(= docker-compose.yml 옆)에 .env 파일 배치
        }
      }
    }

    stage('Build JAR') {                   // 3) 스프링 부트 JAR 빌드 단계
      steps {
        dir('GameDataServer') {            // Dockerfile과 gradlew가 있는 폴더로 이동
          sh 'chmod +x gradlew || true'    // gradlew 실행권한 부여(이미 있으면 무시)
          sh './gradlew clean bootJar'     // 부트 JAR 생성 (우리가 build.gradle로 app.jar 고정함)
        }
      }
    }

    stage('Build & Deploy (docker compose)') { // 4) 컨테이너 빌드/배포 단계
      steps {
        // compose로 api 이미지를 빌드 (docker-compose.yml의 build.context 사용)
        sh 'docker-compose -f docker-compose.yml build api'
        // 서비스들을 원하는 상태로 맞춤(없으면 생성/있으면 갱신). -d: 백그라운드
        // mysql도 정의되어 있으면 함께 켜짐(이미 실행 중이면 변경사항만 반영)
        sh 'docker-compose -f docker-compose.yml up -d'
      }
    }

    stage('Verify') {                      // 5) 기본 검증 단계
      steps {
        sh 'docker-compose -f docker-compose.yml ps'                          // 실행 중인 서비스 목록 확인
        sh 'docker-compose -f docker-compose.yml logs --no-color api | tail -n 80 || true' // API 최근 로그 일부
      }
    }
  }
}
