cd C:\Codigo\project\aia-myanmar-backend

git pull

git checkout uat-release

kubectl delete service clamav-service -n nsp-mm-u-aiaplus01

kubectl delete deployment clamav-deployment -n nsp-mm-u-aiaplus01

$timestamp = Get-Date -Format "ddMMyyyy_HHmmss"

$dockerTag = "aiaplus_clamav:$timestamp"

az acr task run --registry acrmm01seanshared01 --name base_image_builder --file yaml/UAT/ClamAv/Dockerfile --set image_name=$dockerTag --context .

$yamlFilePath = "C:\Codigo\project\aia-myanmar-backend\yaml\UAT\ClamAv\clamav_deployment.yml"

$yamlContent = Get-Content $yamlFilePath -Raw

$pattern = 'image: acrmm01seanshared01\.azurecr\.io/aiaplus_clamav:[^\s]+'

$updatedYamlContent = $yamlContent -replace $pattern, "image: acrmm01seanshared01.azurecr.io/aiaplus_clamav:$timestamp"

$updatedYamlContent | Set-Content $yamlFilePath -NoNewline

cd C:\Codigo\project\aia-myanmar-backend\yaml\UAT\ClamAv

kubectl apply -f $yamlFilePath -n nsp-mm-u-aiaplus01

cd C:\Codigo\project\aia-myanmar-backend

git add $yamlFilePath
git commit -m "ClamAv - Update Docker image tag to $timestamp and Deploy to UAT"
git push