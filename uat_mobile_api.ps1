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

# Build the Docker image with the dynamic timestamp tag
$dockerTag = "aiaplus_mobile_api:$timestamp"
#az acr build --registry acrmm01seanshared01.azurecr.io --image $dockerTag -f mobile_api/Dockerfile .
az acr task run --registry acrmm01seanshared01 --name base_image_builder --file mobile_api/Dockerfile --set image_name=$dockerTag --context .

# Define the path to your mobile.yml file
$yamlFilePath = "C:\Codigo\project\aia-myanmar-backend\yaml\UAT\mobile_api.yml"

# Load the YAML file as a string
$yamlContent = Get-Content $yamlFilePath -Raw

# Define the regular expression pattern to match the image field
$pattern = 'image: acrmm01seanshared01\.azurecr\.io/aiaplus_mobile_api:[^\s]+'

# Replace the Docker image tag in the YAML content using the regular expression
$updatedYamlContent = $yamlContent -replace $pattern, "image: acrmm01seanshared01.azurecr.io/aiaplus_mobile_api:$timestamp"

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
git commit -m "MOBILE - Update Docker image tag to $timestamp and Deploy to UAT"
git push