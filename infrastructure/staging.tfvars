# Staging Environment Configuration
# Usage: terraform apply -var-file=staging.tfvars

location    = "eastus"
environment = "staging"
app_name    = "payment-gateway"

# App Service - Staging config (balanced)
sku_name       = "P1V2"
instance_count = 2
dotnet_version = "8.0"

# Key Vault
enable_key_vault_purge_protection    = true
key_vault_soft_delete_retention_days = 30

# Storage
enable_storage_https_only  = true
storage_min_tls_version    = "TLS1_2"
storage_containers         = ["audit-logs", "backups"]
enable_public_network_access = false

# Monitoring
app_insights_sampling_percentage = 100
log_retention_days               = 60

# Auto-scale
enable_autoscale                      = true
autoscale_min_instances               = 2
autoscale_max_instances               = 6
autoscale_cpu_threshold_scale_out     = 75
autoscale_cpu_threshold_scale_in      = 40

# Bank Service
bank_authorize_endpoint = "https://staging-bank-api.example.com/authorize"

# Backup
enable_backup         = true
backup_retention_days = 30

# Tags
tags = {
  Environment  = "Staging"
  Team         = "Platform"
  CostCenter   = "Engineering"
  Criticality  = "Medium"
  ManagedBy    = "Terraform"
  CreatedDate  = "2024-01-01"
  BackupPolicy = "Daily"
}

# App Settings
app_settings = {
  "ASPNETCORE_ENVIRONMENT"        = "Staging"
  "Logging__LogLevel__Default"    = "Information"
  "Bank__TimeoutSeconds"          = "20"
  "Bank__MaxRetries"              = "2"
  "Bank__CircuitBreakerThreshold" = "4"
}
