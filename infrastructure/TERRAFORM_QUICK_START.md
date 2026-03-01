# Terraform Quick Start - 5 Minutes

## Install & Setup (2 minutes)

```bash
# Install Terraform
brew install terraform  # macOS
# or download from https://www.terraform.io/downloads.html

# Login to Azure
az login

# Verify
terraform version
```

## Deploy (3 minutes)

### Step 1: Initialize
```bash
terraform init
```
Downloads provider and initializes workspace.

### Step 2: Plan
```bash
# Development
terraform plan -var-file=dev.tfvars -out=tfplan

# Production
terraform plan -var-file=prod.tfvars -out=tfplan
```

### Step 3: Apply
```bash
terraform apply tfplan
```

### Step 4: View Outputs
```bash
terraform output
# or specific:
terraform output app_service_url
terraform output key_vault_uri
```

---

## Key Commands

```bash
# Planning
terraform plan -var-file=dev.tfvars          # See what will change
terraform plan -var-file=dev.tfvars -destroy # See what will be deleted

# Execution
terraform apply -var-file=dev.tfvars                    # Apply changes
terraform apply -auto-approve -var-file=dev.tfvars     # Skip approval
terraform destroy -var-file=dev.tfvars                  # Delete everything

# State Management
terraform state list              # List all resources
terraform state show azurerm_resource_group.main  # Show details
terraform refresh                 # Sync state with Azure
terraform state pull > backup.tfstate  # Backup state

# Validation
terraform validate                # Check syntax
terraform fmt                     # Format code
terraform show                    # Show current state

# Debugging
terraform console                 # Interactive shell
export TF_LOG=DEBUG               # Enable debug logging
terraform plan                    # Run with debug enabled
```

---

## File Structure

```
.
├── main.tf              # Infrastructure resources
├── variables.tf         # Input variables
├── dev.tfvars          # Development config
├── staging.tfvars      # Staging config
├── prod.tfvars         # Production config
├── terraform.tfstate   # Current state (local)
├── terraform.tfvars    # Auto-loaded variables
└── .terraform/         # Provider cache (auto-created)
```

---

## Environment-Specific Deployment

### Development
```bash
terraform plan -var-file=dev.tfvars -out=tfplan.dev
terraform apply tfplan.dev
# Result: B2 tier, 1 instance, $80/month
```

### Staging
```bash
terraform plan -var-file=staging.tfvars -out=tfplan.staging
terraform apply tfplan.staging
# Result: P1V2 tier, 2 instances, auto-scale, $200/month
```

### Production
```bash
terraform plan -var-file=prod.tfvars -out=tfplan.prod
terraform apply tfplan.prod
# Result: P1V2 tier, 3 instances, auto-scale, $400+/month
```

---

## Customize Configuration

### Edit Environment File
```bash
# Edit dev.tfvars, staging.tfvars, or prod.tfvars
nano dev.tfvars

# Common changes:
# - location: "westus2"
# - sku_name: "P2V2"
# - instance_count: 5
```

### Override via Command Line
```bash
terraform plan \
  -var-file=prod.tfvars \
  -var="instance_count=5" \
  -var="location=westus2"
```

### Environment Variables
```bash
export TF_VAR_instance_count=2
export TF_VAR_environment=staging
terraform plan
```

---

## Check What Changed

### Before Applying
```bash
terraform plan -var-file=prod.tfvars
# Review output carefully!
# Look for:
# + = will be created
# - = will be deleted
# ~ = will be modified
# - / + = will be replaced
```

### After Applying
```bash
terraform show
terraform state show azurerm_linux_web_app.main
```

---

## Troubleshooting

### Syntax Error?
```bash
terraform validate
terraform fmt  # Auto-format
```

### State Out of Sync?
```bash
terraform refresh
terraform plan
```

### Want to Delete Everything?
```bash
terraform destroy -var-file=prod.tfvars
# Type 'yes' when prompted
```

### Resource Already Exists?
```bash
# Import existing resource
terraform import azurerm_resource_group.main \
  /subscriptions/{sub-id}/resourceGroups/payment-gateway-rg-dev
```

---

## Output Values

After deployment, check:

```bash
# Web app URL
terraform output app_service_url

# Key Vault URI (for app to access secrets)
terraform output key_vault_uri

# Storage account (for logs/backups)
terraform output storage_account_name

# Application Insights (for monitoring)
terraform output app_insights_connection_string

# Get all
terraform output
```

---

## Useful Patterns

### Scale Up
```bash
# In prod.tfvars: change sku_name = "P2V2"
terraform apply -var-file=prod.tfvars
```

### Add More Instances
```bash
# In prod.tfvars: change instance_count = 10
terraform apply -var-file=prod.tfvars
```

### Change Location
```bash
terraform apply \
  -var-file=prod.tfvars \
  -var="location=westus2"
```

### Add a Secret to Key Vault
```bash
# Add to main.tf:
resource "azurerm_key_vault_secret" "my_secret" {
  name         = "MySecret"
  value        = "my-secret-value"
  key_vault_id = azurerm_key_vault.main.id
}

terraform apply -var-file=prod.tfvars
```

---

## Production Checklist

Before going live:

- [ ] Reviewed `terraform plan` output
- [ ] Verified resource names and locations
- [ ] Checked Key Vault secrets are set
- [ ] Confirmed app settings are correct
- [ ] Verified monitoring is enabled
- [ ] Tested auto-scaling settings (prod)
- [ ] Checked backup retention settings
- [ ] Reviewed security settings
- [ ] Confirmed cost estimates
- [ ] Team approval obtained

---

## Cost Estimation

```bash
# Development: ~$80/month
terraform plan -var-file=dev.tfvars

# Staging: ~$200/month
terraform plan -var-file=staging.tfvars

# Production: ~$400+/month
terraform plan -var-file=prod.tfvars
```

---

## Next Steps

1. **Install** - `brew install terraform`
2. **Authenticate** - `az login`
3. **Review files** - Read main.tf and variables.tf
4. **Customize** - Edit dev.tfvars for your setup
5. **Test** - `terraform plan -var-file=dev.tfvars`
6. **Deploy** - `terraform apply -var-file=dev.tfvars`
7. **Verify** - Check Azure Portal
8. **Document** - Add to your team wiki

---

## Common Issues

| Issue | Solution |
|-------|----------|
| `Command not found: terraform` | Install Terraform |
| `Error: Invalid provider arguments` | Run `terraform init` |
| `Error: conflicting values` | Check tfvars file syntax |
| `Error: changes are needed` | Run `terraform plan` first |
| `State locked` | Wait, or `terraform force-unlock` |
| `Resource already exists` | Import it: `terraform import` |

---

## Documentation

- **Full Guide:** TERRAFORM_GUIDE.md
- **Migration Guide:** BICEP_TO_TERRAFORM_MIGRATION.md
- **Terraform Docs:** https://www.terraform.io/docs
- **Azure Provider:** https://registry.terraform.io/providers/hashicorp/azurerm/latest

---

## Success!

When you see:
```
Apply complete! Resources: 8 added, 0 changed, 0 destroyed.

Outputs:

app_service_url = "https://payment-gateway-app-dev.azurewebsites.net"
key_vault_uri = "https://payment-gateway-kv-xxx.vault.azure.net/"
```

🎉 You're done! Your infrastructure is deployed and ready to use.
