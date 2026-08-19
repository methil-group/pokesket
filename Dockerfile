FROM unityci/editor:ubuntu-6000.5.9f1-webgl-3.2.2 as builder

WORKDIR /project

COPY . .

RUN /opt/unity/Editor/Unity \
    -batchmode \
    -nographics \
    -quit \
    -projectPath /project \
    -buildTarget WebGL \
    -executeMethod BuildScript.PerformBuild \
    -logFile /dev/stdout

# ---------- Stage 2: Nginx Server ----------
FROM nginx:alpine
COPY --from=builder /project/Build/WebGL /usr/share/nginx/html

COPY nginx.conf /etc/nginx/nginx.conf

RUN chmod -R 777 /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
