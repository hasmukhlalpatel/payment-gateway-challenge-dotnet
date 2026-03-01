# Payment Gateway - Terraform Variables
# This file defines all input variables for the infrastructure

# ============================================================================
# Basic Configuration
# ============================================================================

variable "location" {
  description = "Azure region where resources will be created"
  type        = string
  default     = "eastus"

  validation {
    condition = contains([
      "eastus", "eastus2", "westus", "westus2", "westus3",
      "northeurope", "westeurope", "uksouth", "ukwest",
      "canadacentral", "canadaeast",
      "australiaeast", "australiasoutheast",
      "japaneast", "japanwest"
    ], var.location)
    error_message = "Location must be a valid Azure region."
  }
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod"
  }
}

variable "app_name" {
  description = "Application name (will be used for resource naming)"
  type        = string
  default     = "payment-gateway"

  validation {
    condition     = length(var.app_name) <= 20 && can(regex("^[a-z0-9-]+$", var.app_name))
    error_message = "App name must be 20 characters or less, lowercase letters, numbers, and hyphens only."
  }
}

# ============================================================================
# App Service Configuration
# ============================================================================

variable "sku_name" {
  description = "App Service Plan SKU (B1, B2, P1V2, P2V2, P3V2)"
  type        = string
  default     = "B2"

  validation {
    condition     = contains(["B1", "B2", "P1V2", "P2V2", "P3V2"], var.sku_name)
    error_message = "SKU must be one of: B1, B2, P1V2, P2V2, P3V2"
  }
}

variable "instance_count" {
  description = "Number of App Service instances (1-10)"
  type        = number
  default     = 1

  validation {
    condition     = var.instance_count >= 1 && var.instance_count <= 10
    error_message = "Instance count must be between 1 and 10."
  }
}

variable "app_settings" {
  description = "Additional app settings to merge with defaults"
  type        = map(string)
  default     = {}
}

variable "dotnet_version" {
  description = "Dotnet version for the app service"
  type        = string
  default     = "8.0"

  validation {
    condition     = contains(["6.0", "7.0", "8.0"], var.dotnet_version)
    error_message = "Dotnet version must be 6.0, 7.0, or 8.0"
  }
}

# ============================================================================
# Key Vault Configuration
# ============================================================================

variable "enable_key_vault_purge_protection" {
  description = "Enable purge protection on Key Vault"
  type        = bool
  default     = false
}

variable "key_vault_secrets" {
  description = "Map of secrets to create in Key Vault"
  type        = map(string)
  sensitive   = true
  default     = {}
}

variable "key_vault_soft_delete_retention_days" {
  description = "Number of days for soft delete retention"
  type        = number
  default     = 7

  validation {
    condition     = var.key_vault_soft_delete_retention_days >= 7 && var.key_vault_soft_delete_retention_days <= 90
    error_message = "Soft delete retention must be between 7 and 90 days."
  }
}

# ============================================================================
# Storage Configuration
# ============================================================================

variable "storage_containers" {
  description = "List of storage containers to create"
  type        = list(string)
  default     = ["audit-logs", "backups"]
}

variable "enable_storage_https_only" {
  description = "Require HTTPS for storage account"
  type        = bool
  default     = true
}

variable "storage_min_tls_version" {
  description = "Minimum TLS version for storage"
  type        = string
  default     = "TLS1_2"

  validation {
    condition     = contains(["TLS1_0", "TLS1_1", "TLS1_2"], var.storage_min_tls_version)
    error_message = "Minimum TLS version must be TLS1_0, TLS1_1, or TLS1_2"
  }
}

# ============================================================================
# Monitoring Configuration
# ============================================================================

variable "app_insights_sampling_percentage" {
  description = "Application Insights sampling percentage (0-100)"
  type        = number
  default     = 100

  validation {
    condition     = var.app_insights_sampling_percentage >= 0 && var.app_insights_sampling_percentage <= 100
    error_message = "Sampling percentage must be between 0 and 100."
  }
}

variable "log_retention_days" {
  description = "Number of days to retain logs"
  type        = number
  default     = 30

  validation {
    condition     = var.log_retention_days >= 7 && var.log_retention_days <= 730
    error_message = "Log retention must be between 7 and 730 days."
  }
}

# ============================================================================
# Network Configuration
# ============================================================================

variable "enable_public_network_access" {
  description = "Enable public network access to storage account"
  type        = bool
  default     = false
}

variable "storage_network_bypass" {
  description = "Services to bypass for storage account network rules"
  type        = list(string)
  default     = ["AzureServices", "Logging", "Metrics"]
}

variable "storage_ip_rules" {
  description = "IP addresses/ranges allowed for storage account"
  type        = list(string)
  default     = []
}

# ============================================================================
# Tagging
# ============================================================================

variable "tags" {
  description = "Additional tags to apply to all resources"
  type        = map(string)
  default = {
    "ManagedBy"     = "Terraform"
    "CostCenter"    = "Engineering"
  }
}

variable "require_owner_tag" {
  description = "Require owner tag on all resources"
  type        = bool
  default     = false
}

# ============================================================================
# Auto-Scale Configuration (Production)
# ============================================================================

variable "enable_autoscale" {
  description = "Enable auto-scaling for App Service (prod only)"
  type        = bool
  default     = true
}

variable "autoscale_min_instances" {
  description = "Minimum instances for auto-scale"
  type        = number
  default     = 1

  validation {
    condition     = var.autoscale_min_instances >= 1 && var.autoscale_min_instances <= 10
    error_message = "Minimum instances must be between 1 and 10."
  }
}

variable "autoscale_max_instances" {
  description = "Maximum instances for auto-scale"
  type        = number
  default     = 10

  validation {
    condition     = var.autoscale_max_instances >= 1 && var.autoscale_max_instances <= 100
    error_message = "Maximum instances must be between 1 and 100."
  }
}

variable "autoscale_cpu_threshold_scale_out" {
  description = "CPU threshold to scale out (%)"
  type        = number
  default     = 70

  validation {
    condition     = var.autoscale_cpu_threshold_scale_out >= 0 && var.autoscale_cpu_threshold_scale_out <= 100
    error_message = "CPU threshold must be between 0 and 100."
  }
}

variable "autoscale_cpu_threshold_scale_in" {
  description = "CPU threshold to scale in (%)"
  type        = number
  default     = 30

  validation {
    condition     = var.autoscale_cpu_threshold_scale_in >= 0 && var.autoscale_cpu_threshold_scale_in <= 100
    error_message = "CPU threshold must be between 0 and 100."
  }
}

# ============================================================================
# Bank Service Configuration
# ============================================================================

variable "bank_authorize_endpoint" {
  description = "Bank authorization API endpoint"
  type        = string
  default     = "https://bank-api.example.com/authorize"

  validation {
    condition     = can(regex("^https://", var.bank_authorize_endpoint))
    error_message = "Bank endpoint must be HTTPS."
  }
}

variable "bank_api_key" {
  description = "Bank API key (sensitive)"
  type        = string
  sensitive   = true
  default     = ""
}

# ============================================================================
# Backup and Disaster Recovery
# ============================================================================

variable "enable_backup" {
  description = "Enable backup for database and storage"
  type        = bool
  default     = true
}

variable "backup_retention_days" {
  description = "Number of days to retain backups"
  type        = number
  default     = 30

  validation {
    condition     = var.backup_retention_days >= 7 && var.backup_retention_days <= 365
    error_message = "Backup retention must be between 7 and 365 days."
  }
}

# ============================================================================
# Deployment Options
# ============================================================================

variable "create_resource_group" {
  description = "Create a new resource group (false if using existing)"
  type        = bool
  default     = true
}

variable "existing_resource_group_name" {
  description = "Name of existing resource group (if create_resource_group is false)"
  type        = string
  default     = ""
}
