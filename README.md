# pulumi
for deploy .net service with dockerfile; - minikube start - minikube docker-env - если cmd то пишем @FOR /f "tokens=*" %i IN ('minikube -p minikube docker-env') DO @%i  - docker build -t tagName -f Dockerfile . (для каждого образа в папке, где лежит Dockerfile). Либо настроить docker-compose.yaml
