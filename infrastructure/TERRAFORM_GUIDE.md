# Terraform Implementation - Payment Gateway Infrastructure

## Overview

This directory contains the complete Terraform configuration for deploying the Payment Gateway infrastructure to Azure, converted from the original Bicep template.

## Files

- `main.tf` - Main infrastructure resources (App Service, Key Vault, Storage, Monitoring)
- `variables.tf` - Input variable definitions with validation
- `dev.tfvars` - Development environment variables
- `staging.tfvars` - Staging environment variables
- `prod.tfvars` - Production environment variables

## Key Changes from Bicep

### Advantages of Terraform:
1. **Modules support** - Can extract reusable modules
2. **Remote state management** - Better collaboration
3. **Dynamic blocks** - More flexible configurations
4. **Better interpolation** - HCL2 language is cleaner
5. **Multi-cloud support** - Easy to add AWS/GCP later

### Structure Differences:
- Bicep: All in one file with parameters
- Terraform: Separated into main.tf, variables.tf, outputs.tf (optional)
- Bicep: Uses `@` for metadata
- Terraform: Uses `variable` and `resource` blocks

---

## Prerequisites

### Required Tools
```bash
# Install Terraform
brew install terraform  # macOS
# or download from https://www.terraform.io/downloads.html

# Install Azure CLI
brew install azure-cli

# Verify installations
terraform version
az version
```

### Azure Setup
```bash
# Login to Azure
az login

# Set default subscription
az account set --subscription "YOUR-SUBSCRIPTION-ID"

# View current subscription
az account show
```

---

## Quick Start

### 1. Initialize Terraform
```bash
terraform init
```

This downloads the Azure provider and initializes the working directory.

### 2. Plan Deployment
```bash
# Dev environment
terraform plan -var-file=dev.tfvars -out=tfplan.dev

# Staging environment
terraform plan -var-file=staging.tfvars -out=tfplan.staging

# Production environment
terraform plan -var-file=prod.tfvars -out=tfplan.prod
```

### 3. Apply Configuration
```bash
# Apply the plan
terraform apply tfplan.dev
# or
terraform apply tfplan.prod
```

### 4. View Outputs
```bash
# After successful apply, view outputs
terraform output

# View specific output
terraform output app_service_url
terraform output key_vault_uri
```

### 5. Destroy Resources (when done)
```bash
terraform destroy -var-file=dev.tfvars
```

---

## Configuration by Environment

### Development (Minimal Cost)
```bash
terraform apply -var-file=dev.tfvars
```
- **Compute:** B2 tier (1 instance, ~$50/month)
- **Auto-scale:** Disabled
- **Backups:** 7 days
- **Logs:** 30 days
- **Bank endpoint:** Localhost (for testing)

### Staging (Production-like)
```bash
terraform apply -var-file=staging.tfvars
```
- **Compute:** P1V2 tier (2 instances, auto-scale 2-6)
- **Auto-scale:** Enabled
- **Backups:** 30 days
- **Logs:** 60 days
- **Bank endpoint:** Staging API

### Production (High Availability)
```bash
terraform apply -var-file=prod.tfvars
```
- **Compute:** P1V2 tier (3 instances, auto-scale 2-10)
- **Auto-scale:** Enabled with aggressive thresholds
- **Backups:** 90 days
- **Logs:** 90 days
- **Bank endpoint:** Production API
- **Purge protection:** Enabled on Key Vault
- **Replication:** Geo-redundant

---

## Common Tasks

### Update App Settings
Edit the environment tfvars file:
```hcl
app_settings = {
  "ASPNETCORE_ENVIRONMENT" = "Production"
  "Bank__TimeoutSeconds" = "15"
  # Add your settings here
}
```

Then apply:
```bash
terraform apply -var-file=prod.tfvars
```

### Scale Up App Service
Modify in `prod.tfvars`:
```hcl
sku_name = "P2V2"  # Was P1V2
```

```bash
terraform apply -var-file=prod.tfvars
```

### Add Key Vault Secret
```bash
# Add to terraform
resource "azurerm_key_vault_secret" "my_secret" {
  name         = "MySecret"
  value        = "secret-value"
  key_vault_id = azurerm_key_vault.main.id
}

terraform apply -var-file=prod.tfvars
```

### View Current State
```bash
# List all resources
terraform state list

# Show specific resource
terraform state show azurerm_linux_web_app.main

# Get all outputs
terraform output
```

### Validate Configuration
```bash
# Check syntax
terraform validate

# Format code
terraform fmt

# Check for issues
terraform plan -var-file=dev.tfvars
```

---

## Remote State Management (Recommended for Production)

### Setup Remote State
```bash
# Create storage account for state
az storage account create \
  --name tfstate \
  --resource-group terraform-state-rg \
  --location eastus \
  --sku Standard_LRS

# Create container
az storage container create \
  --name tfstate \
  --account-name tfstate
```

### Configure Backend
Uncomment in `main.tf`:
```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "tfstate"
    container_name       = "tfstate"
    key                  = "payment-gateway.tfstate"
  }
}
```

### Migrate State
```bash
# Reinitialize with backend
terraform init -migrate-state
```

---

## Variable Customization

### Override via Command Line
```bash
terraform apply -var-file=prod.tfvars \
  -var="instance_count=5" \
  -var="location=westus2"
```

### Create Custom tfvars
```bash
# Copy and modify
cp prod.tfvars custom.tfvars
# Edit custom.tfvars
terraform apply -var-file=custom.tfvars
```

### Environment Variables
```bash
export TF_VAR_instance_count=2
export TF_VAR_environment=prod
terraform apply
```

---

## Monitoring & Troubleshooting

### View Plan Details
```bash
terraform show tfplan.dev
```

### Check Resource State
```bash
terraform state show azurerm_key_vault.main
```

### Debug Output
```bash
# Enable debug logging
export TF_LOG=DEBUG
terraform plan -var-file=dev.tfvars
```

### Common Issues

**Issue: Resource already exists**
```bash
# Import existing resource
terraform import azurerm_resource_group.main /subscriptions/xxx/resourceGroups/payment-gateway-rg-dev
```

**Issue: Changes not applying**
```bash
# Refresh state
terraform refresh
terraform plan
```

**Issue: Locked state**
```bash
# Force unlock (be careful!)
terraform force-unlock <LOCK-ID>
```

---

## Cost Estimation

### Development (~$80/month)
- App Service Plan B2: ~$50
- Application Insights: ~$15
- Storage Account: ~$5
- Key Vault: ~$10

### Staging (~$200/month)
- App Service Plan P1V2: ~$130
- Application Insights: ~$30
- Storage Account: ~$15
- Key Vault: ~$10

### Production (~$400+/month)
- App Service Plan P1V2 (3 instances): ~$390
- Application Insights: ~$50
- Storage Account (geo-redundant): ~$40
- Key Vault: ~$10
- Additional services as needed: variable

---

## Scaling Considerations

### Vertical Scaling (SKU)
```hcl
sku_name = "P2V2"  # More powerful instance
```

### Horizontal Scaling (Instances)
```hcl
instance_count = 5  # More instances
```

### Auto-scaling Rules
```hcl
autoscale_cpu_threshold_scale_out = 70  # Scale at 70% CPU
autoscale_max_instances = 10            # Max 10 instances
```

---

## Security Best Practices

### Sensitive Variables
```bash
# Store in environment variables (not in code)
export TF_VAR_bank_api_key="your-api-key"

# Or use terraform.tfvars.local (git-ignored)
echo "bank_api_key = \"your-api-key\"" > terraform.tfvars.local
```

### Restrict State Access
```bash
# Set storage account access
az storage account update \
  --name tfstate \
  --https-only true \
  --require-infrastructure-encryption true
```

### Audit Logging
```hcl
# Terraform automatically logs changes
# View in Azure Activity Log
```

---

## Backup & Disaster Recovery

### Backup State File
```bash
# Manually backup
az storage blob download \
  --container-name tfstate \
  --name payment-gateway.tfstate \
  --file backup.tfstate \
  --account-name tfstate
```

### Recovery
```bash
# Restore from backup
az storage blob upload \
  --container-name tfstate \
  --name payment-gateway.tfstate \
  --file backup.tfstate \
  --account-name tfstate
```

---

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Terraform Deploy

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - uses: hashicorp/setup-terraform@v2
      
      - name: Terraform Init
        run: terraform init
      
      - name: Terraform Plan
        run: terraform plan -var-file=prod.tfvars -out=tfplan
      
      - name: Terraform Apply
        run: terraform apply tfplan
```

---

## Advanced Features

### Workspaces (Multiple Environments)
```bash
# Create workspace
terraform workspace new prod
terraform workspace new staging

# Switch workspace
terraform workspace select prod

# List workspaces
terraform workspace list
```

### Modules (Reusable Components)
```bash
# Create module directory
mkdir -p modules/app_service

# Move resources into module
# Use module in main.tf:
module "app_service" {
  source = "./modules/app_service"
  
  app_name = var.app_name
  location = var.location
}
```

### Locals (Computed Values)
```hcl
locals {
  environment_tag = "${var.app_name}-${var.environment}"
  resource_prefix = "rg-${locals.environment_tag}"
}
```

---

## Testing & Validation

### Pre-deployment Checks
```bash
# Validate syntax
terraform validate

# Format check
terraform fmt -check

# Run tflint (optional)
brew install tflint
tflint
```

### Plan Review
```bash
# Generate detailed plan
terraform plan -var-file=prod.tfvars -out=tfplan

# Review plan
terraform show tfplan

# Or in JSON
terraform show -json tfplan | jq
```

---

## Documentation Generation

### Auto-generate Documentation
```bash
# Install terraform-docs
brew install terraform-docs

# Generate README
terraform-docs markdown . > README.md
```

---

## Rollback & Undo

### View History
```bash
# Show state history
terraform state list
```

### Rollback Change
```bash
# Revert to previous state version
terraform state pull > current.tfstate
# Edit or restore from backup
terraform state push previous.tfstate
```

---

## Useful Commands Reference

```bash
# Initialization
terraform init                           # Initialize workspace
terraform init -upgrade                  # Upgrade provider versions

# Validation & Planning
terraform validate                       # Check syntax
terraform fmt                            # Format code
terraform plan                           # Show what will change
terraform plan -destroy                  # Show what will be deleted

# Execution
terraform apply                          # Apply changes
terraform apply -auto-approve            # Skip approval (CI/CD)
terraform destroy                        # Delete resources

# State Management
terraform state list                     # List resources
terraform state show <resource>          # Show resource details
terraform state pull                     # Download state file
terraform state push <file>              # Upload state file

# Output
terraform output                         # Show outputs
terraform output <name>                  # Show specific output
terraform output -json                   # JSON format

# Debugging
terraform console                        # Interactive console
terraform graph                          # Show dependency graph
TF_LOG=DEBUG terraform plan               # Debug logging
```

---

## Next Steps

1. **Customize variables** - Edit the tfvars files for your environment
2. **Add secrets** - Configure Key Vault secrets in main.tf
3. **Setup remote state** - Use Azure Storage for state management
4. **Configure CI/CD** - Add GitHub Actions or Azure DevOps
5. **Add monitoring** - Configure alerts and dashboards
6. **Document** - Add comments and README

---

## Support & Resources

- **Terraform Docs:** https://www.terraform.io/docs
- **Azure Provider Docs:** https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs
- **Azure Learning:** https://learn.microsoft.com/en-us/azure/
- **Terraform Registry:** https://registry.terraform.io/

---

## Comparison: Bicep vs Terraform

| Feature | Bicep | Terraform |
|---------|-------|-----------|
| Language | Azure-specific DSL | HCL (universal) |
| Learning Curve | Easy (Azure-focused) | Moderate |
| Multi-cloud | No | Yes |
| Modules | Limited | Excellent |
| Remote State | Via Deployment Stacks | Built-in |
| Community | Growing | Large |
| IDE Support | VS Code extension | Excellent |

---

## Migration from Bicep

If migrating from the original Bicep template:

1. **State import** (if resources already exist):
```bash
terraform import azurerm_resource_group.main /subscriptions/xxx/resourceGroups/payment-gateway-rg-dev
```

2. **Verification**:
```bash
terraform plan  # Should show no changes
```

3. **Destroy old resources** (if applicable):
```bash
# Use Azure CLI or Azure Portal to remove old Bicep deployments
```

---

This Terraform configuration provides a complete, production-ready infrastructure setup with the flexibility and power of Infrastructure as Code!
