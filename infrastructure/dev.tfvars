# Development Environment Configuration
# Usage: terraform apply -var-file=dev.tfvars

location    = "eastus"
environment = "dev"
app_name    = "payment-gateway"

# App Service - Minimal config for dev
sku_name       = "B2"
instance_count = 1
dotnet_version = "8.0"

# Key Vault
enable_key_vault_purge_protection    = false
key_vault_soft_delete_retention_days = 7

# Storage
enable_storage_https_only  = true
storage_min_tls_version    = "TLS1_2"
storage_containers         = ["audit-logs", "backups"]
enable_public_network_access = false

# Monitoring
app_insights_sampling_percentage = 100
log_retention_days               = 30

# Auto-scale (disabled for dev to save costs)
enable_autoscale = false

# Bank Service
bank_authorize_endpoint = "http://localhost:8080/authorize"

# Backup
enable_backup         = true
backup_retention_days = 7

# Tags
tags = {
  Environment  = "Development"
  Team         = "Platform"
  CostCenter   = "Engineering"
  ManagedBy    = "Terraform"
  CreatedDate  = "2024-01-01"
}

# App Settings
app_settings = {
  "ASPNETCORE_ENVIRONMENT" = "Development"
  "Logging__LogLevel__Default" = "Debug"
  "Bank__TimeoutSeconds" = "30"
  "Bank__MaxRetries" = "3"
}
