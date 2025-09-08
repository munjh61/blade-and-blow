FROM openjdk:17-jdk
WORKDIR /app
ENV TZ=Asia/Seoul
COPY app.jar /app/app.jar
EXPOSE 8080
ENTRYPOINT ["java","-Xms128m","-Xmx512m","-jar","/app/app.jar"]