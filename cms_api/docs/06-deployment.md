# CMS API - Deployment

## Code Repo && Branches
| Environment | GitHub | Branch |
|----------|----------|----------|
| UAT | https://github.com/sgcodigo/aia-myanmar-backend | uat-release|
| Production | https://github.com/sgcodigo/aia-myanmar-backend | prod-release|


## Deployment Files and Powershell File Paths

| Project | Environment | File Type | Path |
|----------|----------|----------|---------|
| ClamAv | UAT | Powershell | /aia-myanmar-backend/yaml/UAT/ClamAv/uat_clamav.ps1 |
| ClamAv | UAT | Dockerfile | /aia-myanmar-backend/yaml/UAT/ClamAv/Dockerfile |
| ClamAv | UAT | Deployment & Service | /aia-myanmar-backend/yaml/UAT/ClamAv/clamav_deployment.yml |
| cms_api | UAT | Powershell | /aia-myanmar-backend/uat_cms_api.ps1 |
| cms_api | UAT | Dockerfile | /aia-myanmar-backend/cms_api/Dockerfile |
| cms_api | UAT | Deployment & Service | /aia-myanmar-backend/yaml/UAT/cms_api.yml |
| cms_api | UAT | Ingress | /aia-myanmar-backend/yaml/UAT/cms_api_ingress.yml |
|  |  |  |  |
| ClamAv | Production | Powershell | /aia-myanmar-backend/yaml/Production/ClamAv/prod_clamav.ps1 |
| ClamAv | Production | Dockerfile | /aia-myanmar-backend/yaml/Production/ClamAv/Dockerfile |
| ClamAv | Production | Deployment & Service | /aia-myanmar-backend/yaml/Production/ClamAv/clamav_deployment.yml |
| cms_api | Production | Powershell | /aia-myanmar-backend/powershell/Production/cms_api.ps1 |
| cms_api | Production | Dockerfile | /aia-myanmar-backend/cms_api/Dockerfile |
| cms_api | Production | Deployment & Service | /aia-myanmar-backend/yaml/prod_clamav/cms_api.yml |
| cms_api | Production | Ingress | /aia-myanmar-backend/yaml/prod_clamav/cms_api_ingress.yml |

## Place Holder Variables
| Name | UAT | Production |
|----------|----------|----------|
| {github branch} | uat-release | prod-release |
| {jumphost drive path} | C:/Codigo/project/ | C:/Codigo/project/ |
| {azurecr} | acrmm01seanshared01 | acrmm01seapshared01 |
| {namespace} | nsp-mm-u-aiaplus01 | nsp-mm-p-aiaplus01 |
| {environment} | Uat | Production |
| {persistance volumn name} | aiaplus-uat-azure-file-pvc | pvc-mm01-sea-p-apps01-datadisk01 |
| {hostname} | uat.aia.com.mm | plus.aia.com.mm |
| {prefix route} | /aiaplus/cms-api | /cms-api |

### Dockerfile

The application uses a multi-stage Docker build:

```dockerfile
# Build stage
FROM acrgo01easnshared01.azurecr.io/platform/microsoft-dotnet-aspnet:8-standard AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

#RUN apk add --no-cache icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
USER root
RUN apk add --no-cache icu-libs krb5-libs libgcc libintl libssl3 libstdc++ zlib
RUN apk update
RUN apk add bash
		
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["aia_core/aia_core.csproj", "aia_core/"]
COPY ["cms_api/cms_api.csproj", "cms_api/"]
RUN dotnet restore "cms_api/cms_api.csproj"
COPY . .
WORKDIR "/src/cms_api"
RUN dotnet build "cms_api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "cms_api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

# Create the system group 'appgroup' if it does not exist
RUN addgroup -S appgroup || true

# Check if 'appuser' exists, and create it if it does not
RUN id appuser >/dev/null 2>&1 || adduser -S -G appgroup -u 1001 appuser

# Create the app directory and set ownership
RUN chown -R appuser:appgroup /app

ENTRYPOINT ["dotnet", "cms_api.dll"]
```
### Powershell

```powershell
# Change directory to git
cd {jumphost drive path}\aia-myanmar-backend

# Pull the latest changes from Git
git pull

# Checkout the uat-release branch
git checkout {github branch}

# Pull the latest changes from Git
git pull

# Checkout the uat-release branch
git checkout {github branch}

# Get the current timestamp in the format DDMMYYYY_HHMMSS
$timestamp = Get-Date -Format "ddMMyyyy_HHmmss"

#az acr repository show-tags --name {azurecr} --repository aiaplus_cms_api --output table

#kubectl delete service aia-cms-api -n {namespace}
#kubectl delete deployment aia-cms-api-deployment -n {namespace}

# Build the Docker image with the dynamic timestamp tag
$dockerTag = "aiaplus_cms_api:$timestamp"
#az acr build --registry {azurecr}.azurecr.io --image $dockerTag -f cms_api/Dockerfile .
az acr task run --registry {azurecr} --name base_image_builder --file cms_api/Dockerfile --set image_name=$dockerTag --context .

# Define the path to your cms.yml file
$yamlFilePath = "{jumphost drive path}\aia-myanmar-backend\yaml\UAT\cms_api.yml"

# Load the YAML file as a string
$yamlContent = Get-Content $yamlFilePath -Raw

# Define the regular expression pattern to match the image field
$pattern = 'image: {azurecr}\.azurecr\.io/aiaplus_cms_api:[^\s]+'

# Replace the Docker image tag in the YAML content using the regular expression
$updatedYamlContent = $yamlContent -replace $pattern, "image: {azurecr}.azurecr.io/aiaplus_cms_api:$timestamp"

# Write the updated YAML content back to the file
$updatedYamlContent | Set-Content $yamlFilePath -NoNewline

# Change directory to yaml UAT Folder
cd {jumphost drive path}\aia-myanmar-backend\yaml\UAT

# Apply Kubernetes configuration
kubectl apply -f $yamlFilePath -n nsp-mm-u-aiaplus01

# Change directory to git
cd {jumphost drive path}\aia-myanmar-backend

# Commit and push the changes
git add $yamlFilePath
git commit -m "CMS - Update Docker image tag to $timestamp and Deploy to UAT"
git push
```

### Deployment YAML

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: aia-cms-api-deployment
spec:
  replicas: 1  # You can adjust the number of replicas as needed
  selector:
    matchLabels:
      app: aia-cms-api
  template:
    metadata:
      labels:
        app: aia-cms-api
    spec:
      securityContext:
        runAsNonRoot: true  # Pod must not run as root
        runAsUser: 1001     # Run pod as non-root UID 1001
      containers:
        - name: aia-cms-api
          image:  {azurecr}.azurecr.io/aiaplus_cms_api:{DDMMYYYY_HHMMSS}
          ports:
            - containerPort: 8080
          securityContext:
            readOnlyRootFilesystem: true     # Makes root filesystem read-only
            allowPrivilegeEscalation: false  # No privilege escalation
            privileged: false                # Ensure it doesn't run in privileged mode
            capabilities:
              drop:
                - ALL
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "{environment}"
          volumeMounts:
          - mountPath: "/app/wwwroot/images"
            name: volume
          - mountPath: "/tmp"
            name: tmp-dir
      volumes:
      - name: volume
        persistentVolumeClaim:
          claimName: {persistance volumn name}
      - name: tmp-dir
        emptyDir: {}
```

### Service YAML

```yaml
apiVersion: v1
kind: Service
metadata:
  labels:
    app: aia-cms-api
  name: aia-cms-api
  namespace: {namespace}
spec:
  ports:
    - port: 80
      protocol: TCP
      targetPort: 8080
  selector:
    app: aia-cms-api
  type: ClusterIP
```

### Ingress YAML

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: cms-api-ingress
  namespace: {namespace}
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "false"
    nginx.ingress.kubernetes.io/rewrite-target: /$2
    nginx.ingress.kubernetes.io/use-regex: "true"
    nginx.ingress.kubernetes.io/proxy-body-size: "50m"
spec:
  ingressClassName: nginx-ingress-controller #nginx-u-aiaplus01
  tls:
   - hosts:
       - {hostname}
  rules:
     - host: {hostname}
       http:
        paths:
          - path: {prefix route}(/|$)(.*) # Replace with your desired route prefix
            pathType: ImplementationSpecific
            backend:
              service:
                name: aia-cms-api # Replace with the name of your backend service
                port:
                  number: 80 # Replace with the port number of your backend service
```

### Deploy using powershell file (Using Powershell Console, PowerShell ISE)
1. az login
2. kubectl get pods -n {namespace}
3. Run the powershell .ps1 file for the auto step by step deployment
4. Verify the deployment is successful. Type again kubectl get pods -n {namespace}
   
### Verify the deployment Status
1. kubectl decribe pod {pod name} -n {namespace}

### Export application log to file
1. kubectl logs {pod name} -n {namespace} > {jumphost drive path}\{logfile_name}.txt

### Enter the pod file system
1. kubectl exec -it {pod name} -n {namespace} -- /bin/bash
   
### Checking log in Azure Portal for Long Period logs
1. Login to https://portal.azure.com
2. Go to container log section under Kubenetes Services
3. Write log query language to select the log by filter name (Log query is similar to SQL format in general)
