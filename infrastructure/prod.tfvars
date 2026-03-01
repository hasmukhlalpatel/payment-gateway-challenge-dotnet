# Production Environment Configuration
# Usage: terraform apply -var-file=prod.tfvars

location    = "eastus"
environment = "prod"
app_name    = "payment-gateway"

# App Service - Production config
sku_name       = "P1V2"
instance_count = 3
dotnet_version = "8.0"

# Key Vault
enable_key_vault_purge_protection    = true
key_vault_soft_delete_retention_days = 90

# Storage
enable_storage_https_only  = true
storage_min_tls_version    = "TLS1_2"
storage_containers         = ["audit-logs", "backups", "disaster-recovery"]
enable_public_network_access = false

# Monitoring
app_insights_sampling_percentage = 100
log_retention_days               = 90

# Auto-scale
enable_autoscale                      = true
autoscale_min_instances               = 2
autoscale_max_instances               = 10
autoscale_cpu_threshold_scale_out     = 70
autoscale_cpu_threshold_scale_in      = 30

# Bank Service
bank_authorize_endpoint = "https://bank-api.example.com/authorize"

# Backup
enable_backup         = true
backup_retention_days = 90

# Tags
tags = {
  Environment  = "Production"
  Team         = "Platform"
  CostCenter   = "Operations"
  Criticality  = "High"
  ManagedBy    = "Terraform"
  CreatedDate  = "2024-01-01"
  BackupPolicy = "Daily"
  Compliance   = "PCI-DSS"
}

# App Settings
app_settings = {
  "ASPNETCORE_ENVIRONMENT"        = "Production"
  "Logging__LogLevel__Default"    = "Warning"
  "Bank__TimeoutSeconds"          = "15"
  "Bank__MaxRetries"              = "2"
  "Bank__CircuitBreakerThreshold" = "3"
  "WEBSITE_ENABLE_SYNC_UPDATE_SITE" = "true"
}
