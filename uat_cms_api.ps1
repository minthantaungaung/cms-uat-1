# Change directory to git
cd C:\Codigo\project\aia-myanmar-backend

# Pull the latest changes from Git
git pull

# Checkout the uat-release branch
git checkout uat-release

# Pull the latest changes from Git
git pull

# Checkout the uat-release branch
git checkout uat-release

# Get the current timestamp in the format DDMMYYYY_HHMMSS
$timestamp = Get-Date -Format "ddMMyyyy_HHmmss"

#az acr repository show-tags --name acrmm01seanshared01 --repository aiaplus_cms_api --output table

#kubectl delete service aia-cms-api -n nsp-mm-u-aiaplus01
#kubectl delete deployment aia-cms-api-deployment -n nsp-mm-u-aiaplus01

# Build the Docker image with the dynamic timestamp tag
$dockerTag = "aiaplus_cms_api:$timestamp"
#az acr build --registry acrmm01seanshared01.azurecr.io --image $dockerTag -f cms_api/Dockerfile .
az acr task run --registry acrmm01seanshared01 --name base_image_builder --file cms_api/Dockerfile --set image_name=$dockerTag --context .

# Define the path to your cms.yml file
$yamlFilePath = "C:\Codigo\project\aia-myanmar-backend\yaml\UAT\cms_api.yml"

# Load the YAML file as a string
$yamlContent = Get-Content $yamlFilePath -Raw

# Define the regular expression pattern to match the image field
$pattern = 'image: acrmm01seanshared01\.azurecr\.io/aiaplus_cms_api:[^\s]+'

# Replace the Docker image tag in the YAML content using the regular expression
$updatedYamlContent = $yamlContent -replace $pattern, "image: acrmm01seanshared01.azurecr.io/aiaplus_cms_api:$timestamp"

# Write the updated YAML content back to the file
$updatedYamlContent | Set-Content $yamlFilePath -NoNewline

# Change directory to yaml UAT Folder
cd C:\Codigo\project\aia-myanmar-backend\yaml\UAT

# Apply Kubernetes configuration
kubectl apply -f $yamlFilePath -n nsp-mm-u-aiaplus01

# Change directory to git
cd C:\Codigo\project\aia-myanmar-backend

# Commit and push the changes
git add $yamlFilePath
git commit -m "CMS - Update Docker image tag to $timestamp and Deploy to UAT"
git push