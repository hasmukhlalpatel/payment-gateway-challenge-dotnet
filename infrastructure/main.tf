# Payment Gateway Infrastructure - Terraform
# Converts Azure Bicep template to Terraform

terraform {
  required_version = ">= 1.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }

  # Uncomment to use remote state (recommended for production)
  # backend "azurerm" {
  #   resource_group_name  = "terraform-state-rg"
  #   storage_account_name = "tfstate"
  #   container_name       = "tfstate"
  #   key                  = "payment-gateway.tfstate"
  # }
}

provider "azurerm" {
  features {}
}

# ============================================================================
# Data Sources
# ============================================================================

data "azurerm_client_config" "current" {}

# ============================================================================
# Variables (Input Parameters)
# ============================================================================

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "eastus"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
  
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "app_name" {
  description = "Application name"
  type        = string
  default     = "payment-gateway"
}

variable "sku_name" {
  description = "App Service Plan SKU"
  type        = string
  default     = "B2"
  
  validation {
    condition     = contains(["B1", "B2", "P1V2", "P2V2", "P3V2"], var.sku_name)
    error_message = "SKU must be B1, B2, P1V2, P2V2, or P3V2."
  }
}

variable "instance_count" {
  description = "Number of App Service instances"
  type        = number
  default     = 1
  
  validation {
    condition     = var.instance_count >= 1 && var.instance_count <= 10
    error_message = "Instance count must be between 1 and 10."
  }
}

variable "tags" {
  description = "Common tags for all resources"
  type        = map(string)
  default = {
    "Managed-By" = "Terraform"
  }
}

# ============================================================================
# Locals (Computed Values)
# ============================================================================

locals {
  resource_prefix = "${replace(var.app_name, "-", "")}-${var.environment}"
  
  common_tags = merge(
    var.tags,
    {
      Environment = var.environment
      Application = var.app_name
      CreatedDate = timestamp()
    }
  )

  app_service_plan_name     = "${var.app_name}-asp-${var.environment}"
  web_app_name              = "${var.app_name}-app-${var.environment}"
  app_insights_name         = "${var.app_name}-ai-${var.environment}"
  key_vault_name            = "${local.resource_prefix}-kv-${substr(md5(data.azurerm_client_config.current.subscription_id), 0, 8)}"
  storage_account_name      = "${replace(local.resource_prefix, "-", "")}st${substr(md5(data.azurerm_client_config.current.subscription_id), 0, 8)}"
  log_analytics_workspace   = "${var.app_name}-law-${var.environment}"
  
  # Determine capacity based on environment
  app_service_capacity = var.environment == "prod" ? 3 : var.instance_count
  
  # Determine storage replication based on environment
  storage_replication_type = var.environment == "prod" ? "RAGRS" : "LRS"
}

# ============================================================================
# Resource Group
# ============================================================================

resource "azurerm_resource_group" "main" {
  name       = "${var.app_name}-rg-${var.environment}"
  location   = var.location
  
  tags = local.common_tags
}

# ============================================================================
# App Service Plan
# ============================================================================

resource "azurerm_service_plan" "main" {
  name                = local.app_service_plan_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  os_type             = "Linux"
  sku_name            = var.sku_name

  # Configure reserved capacity based on SKU
  reserved = true

  tags = local.common_tags
}

# ============================================================================
# Web App (App Service)
# ============================================================================

resource "azurerm_linux_web_app" "main" {
  name                = local.web_app_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.main.id

  https_only = true

  # Managed Identity
  identity {
    type = "SystemAssigned"
  }

  # Site Configuration
  site_config {
    dotnet_version              = "8.0"
    always_on                   = true
    minimum_tls_version         = "1.2"
    managed_pipeline_mode       = "Integrated"
    app_command_line            = ""
    container_registry_use_managed_identity = true

    # Application Settings
    app_settings = {
      "ASPNETCORE_ENVIRONMENT"                  = var.environment
      "APPLICATIONINSIGHTS_CONNECTION_STRING"   = azurerm_application_insights.main.connection_string
      "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
      "XDT_MicrosoftApplicationInsights_Mode"   = "recommended"
      "Bank__AuthorizeEndpoint"                 = "https://bank-api.example.com/authorize"
      "KeyVault__VaultUri"                      = azurerm_key_vault.main.vault_uri
      "WEBSITE_RUN_FROM_PACKAGE"                = "1"
    }

    # Connection Strings
    connection_string {
      name             = "AzureKeyVaultUri"
      value            = azurerm_key_vault.main.vault_uri
      type             = "Custom"
    }

    # Logging
    application_logs {
      file_system_level = "Verbose"
    }
  }

  # Application Insights Link
  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"          = azurerm_application_insights.main.instrumentation_key
    "APPINSIGHTS_PROFILERFEATURE_VERSION"     = "1.0.0"
    "APPINSIGHTS_SNAPSHOTFEATURE_VERSION"     = "1.0.0"
    "DiagnosticServices_EXTENSION_VERSION"    = "~3"
    "InstrumentationEngine_EXTENSION_VERSION" = "disabled"
    "SnapshotDebugger_EXTENSION_VERSION"      = "disabled"
    "XDT_MicrosoftApplicationInsights_BaseExtensions" = "disabled"
  }

  tags = local.common_tags

  depends_on = [
    azurerm_application_insights.main,
    azurerm_key_vault.main
  ]
}

# ============================================================================
# App Service Plan Auto-Scale Settings (Production)
# ============================================================================

resource "azurerm_monitor_autoscale_setting" "app_service" {
  count = var.environment == "prod" ? 1 : 0
  
  name                = "${local.web_app_name}-autoscale"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  target_resource_id  = azurerm_service_plan.main.id

  profile {
    name = "Auto scale based on CPU"

    capacity {
      default = 1
      minimum = 1
      maximum = 10
    }

    rule {
      metric_trigger {
        metric_name              = "CpuPercentage"
        metric_resource_id       = azurerm_service_plan.main.id
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT5M"
        time_aggregation         = "Average"
        operator                 = "GreaterThan"
        threshold                = 70
      }

      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = 1
        cooldown  = "PT5M"
      }
    }

    rule {
      metric_trigger {
        metric_name        = "CpuPercentage"
        metric_resource_id = azurerm_service_plan.main.id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT5M"
        time_aggregation   = "Average"
        operator           = "LessThan"
        threshold          = 30
      }

      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = 1
        cooldown  = "PT10M"
      }
    }
  }

  enabled = true

  depends_on = [azurerm_linux_web_app.main]
}

# ============================================================================
# Application Insights
# ============================================================================

resource "azurerm_application_insights" "main" {
  name                = local.app_insights_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"

  retention_in_days                         = var.environment == "prod" ? 90 : 30
  sampling_percentage                       = 100
  disable_ip_masking                        = false
  force_customer_storage_for_profiler       = false
  force_customer_storage_for_profiler_queue = false

  tags = local.common_tags
}

# ============================================================================
# Log Analytics Workspace
# ============================================================================

resource "azurerm_log_analytics_workspace" "main" {
  name                = local.log_analytics_workspace
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = var.environment == "prod" ? 90 : 30

  tags = local.common_tags
}

# ============================================================================
# Diagnostic Settings for App Service
# ============================================================================

resource "azurerm_monitor_diagnostic_setting" "app_service" {
  name               = "${local.web_app_name}-diagnostics"
  target_resource_id = azurerm_linux_web_app.main.id
  workspace_id       = azurerm_log_analytics_workspace.main.id

  enabled_log {
    category = "AppServiceHTTPLogs"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.environment == "prod" ? 90 : 30
    }
  }

  enabled_log {
    category = "AppServiceConsoleLogs"
    enabled  = true
  }

  enabled_log {
    category = "AppServiceAppLogs"
    enabled  = true
  }

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.environment == "prod" ? 90 : 30
    }
  }
}

# ============================================================================
# Key Vault
# ============================================================================

resource "azurerm_key_vault" "main" {
  name                = local.key_vault_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tenant_id       = data.azurerm_client_config.current.tenant_id
  sku_name        = "standard"

  enabled_for_deployment            = true
  enabled_for_template_deployment   = true
  enabled_for_disk_encryption       = false
  enable_rbac_authorization         = false
  purge_protection_enabled          = var.environment == "prod" ? true : false
  soft_delete_retention_days        = 7

  network_acls {
    default_action = "Allow"
    bypass         = ["AzureServices"]
  }

  tags = local.common_tags
}

# ============================================================================
# Key Vault Access Policy for App Service
# ============================================================================

resource "azurerm_key_vault_access_policy" "app_service" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_web_app.main.identity[0].principal_id

  key_permissions = [
    "Get",
    "List"
  ]

  secret_permissions = [
    "Get",
    "List"
  ]

  certificate_permissions = [
    "Get",
    "List"
  ]
}

# ============================================================================
# Key Vault Secrets (Examples - Update with actual values)
# ============================================================================

resource "azurerm_key_vault_secret" "bank_endpoint" {
  name            = "BankAuthorizationEndpoint"
  value           = "https://bank-api.example.com/authorize"
  key_vault_id    = azurerm_key_vault.main.id
  content_type    = "text/plain"
  expiration_date = timeadd(timestamp(), "8760h") # 1 year
}

resource "azurerm_key_vault_secret" "bank_api_key" {
  name            = "BankApiKey"
  value           = "your-api-key-here" # Replace with actual
  key_vault_id    = azurerm_key_vault.main.id
  content_type    = "text/plain"
  expiration_date = timeadd(timestamp(), "8760h")
}

resource "azurerm_key_vault_secret" "encryption_key" {
  name            = "EncryptionKey"
  value           = "your-encryption-key-here" # Replace with actual
  key_vault_id    = azurerm_key_vault.main.id
  content_type    = "text/plain"
  expiration_date = timeadd(timestamp(), "8760h")
}

# ============================================================================
# Storage Account (for audit logs and backups)
# ============================================================================

resource "azurerm_storage_account" "main" {
  name                     = local.storage_account_name
  location                 = azurerm_resource_group.main.location
  resource_group_name      = azurerm_resource_group.main.name
  account_tier             = "Standard"
  account_replication_type = local.storage_replication_type
  access_tier              = "Hot"
  https_traffic_only_enabled = true
  min_tls_version          = "TLS1_2"
  public_network_access_enabled = false
  shared_access_key_enabled = true

  tags = local.common_tags
}

# ============================================================================
# Storage Account Network Rules
# ============================================================================

resource "azurerm_storage_account_network_rules" "main" {
  storage_account_id = azurerm_storage_account.main.id

  default_action             = "Allow"
  bypass                     = ["AzureServices", "Logging", "Metrics"]
  virtual_network_subnet_ids = []
  ip_rules                   = []
}

# ============================================================================
# Blob Container for Audit Logs
# ============================================================================

resource "azurerm_storage_container" "audit_logs" {
  name                  = "audit-logs"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "backups" {
  name                  = "backups"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

# ============================================================================
# Outputs
# ============================================================================

output "app_service_url" {
  description = "URL of the deployed App Service"
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
}

output "app_service_name" {
  description = "Name of the App Service"
  value       = azurerm_linux_web_app.main.name
}

output "app_insights_instrumentation_key" {
  description = "Application Insights instrumentation key"
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}

output "app_insights_connection_string" {
  description = "Application Insights connection string"
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}

output "key_vault_id" {
  description = "Key Vault resource ID"
  value       = azurerm_key_vault.main.id
}

output "key_vault_uri" {
  description = "Key Vault URI"
  value       = azurerm_key_vault.main.vault_uri
}

output "storage_account_id" {
  description = "Storage Account resource ID"
  value       = azurerm_storage_account.main.id
}

output "storage_account_name" {
  description = "Storage Account name"
  value       = azurerm_storage_account.main.name
}

output "log_analytics_workspace_id" {
  description = "Log Analytics Workspace ID"
  value       = azurerm_log_analytics_workspace.main.id
}

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "resource_group_id" {
  description = "ID of the resource group"
  value       = azurerm_resource_group.main.id
}

output "web_app_identity_principal_id" {
  description = "Principal ID of the Web App managed identity"
  value       = azurerm_linux_web_app.main.identity[0].principal_id
}
