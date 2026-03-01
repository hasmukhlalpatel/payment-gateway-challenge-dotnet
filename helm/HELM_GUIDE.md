# Helm Chart for Payment Gateway on Kubernetes/AKS
## Complete Guide and Usage Documentation

---

## Overview

This Helm chart deploys the Payment Gateway microservice to Kubernetes (AKS, EKS, GKE, etc.) with:

- **Production-ready configurations** for dev, staging, and production environments
- **High availability** with replicas, auto-scaling, and pod disruption budgets
- **Security** with network policies, RBAC, security contexts, and secrets
- **Observability** with health checks, metrics, and logging
- **Resilience** with liveness/readiness/startup probes and graceful shutdown

---

## Prerequisites

### Required Software
```bash
# Kubernetes cluster (1.24+)
# Install Helm 3.x
brew install helm

# Verify
helm version
kubectl version
```

### AKS Setup
```bash
# Create AKS cluster
az aks create \
  --resource-group payment-gateway-rg \
  --name payment-gateway-aks \
  --node-count 3 \
  --vm-set-type VirtualMachineScaleSets \
  --load-balancer-sku standard \
  --enable-managed-identity \
  --network-plugin azure

# Get credentials
az aks get-credentials \
  --resource-group payment-gateway-rg \
  --name payment-gateway-aks

# Verify connection
kubectl cluster-info
kubectl get nodes
```

---

## File Structure

```
payment-gateway/
├── Chart.yaml                    # Chart metadata
├── values.yaml                   # Default values
├── values-dev.yaml              # Development overrides
├── values-staging.yaml          # Staging overrides
├── values-prod.yaml             # Production overrides
├── templates/
│   ├── _helpers.tpl             # Template helpers
│   ├── deployment.yaml          # Main deployment
│   ├── service.yaml             # Kubernetes service
│   ├── configmap.yaml           # Configuration
│   ├── secret.yaml              # Secrets
│   ├── ingress.yaml             # Ingress rules
│   ├── hpa.yaml                 # Auto-scaling
│   ├── pdb.yaml                 # Pod disruption budget
│   ├── networkpolicy.yaml       # Network policies
│   ├── serviceaccount.yaml      # Service account
│   ├── rbac.yaml                # RBAC rules
│   ├── tests/
│   │   └── test-connection.yaml # Connection test
│   └── NOTES.txt                # Post-install notes
└── README.md                     # Chart documentation
```

---

## Quick Start (5 minutes)

### Step 1: Add Helm Repository (if using private registry)
```bash
helm repo add payment-gateway https://charts.company.com/payment-gateway
helm repo update
```

### Step 2: Create Namespace
```bash
kubectl create namespace payment-gateway-dev
```

### Step 3: Install Chart
```bash
# Development
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-dev \
  -f payment-gateway/values-dev.yaml

# Or using Helm repo
helm install payment-gateway payment-gateway/payment-gateway \
  --namespace payment-gateway-dev \
  -f values-dev.yaml
```

### Step 4: Verify Installation
```bash
helm list -n payment-gateway-dev
kubectl get pods -n payment-gateway-dev
kubectl get svc -n payment-gateway-dev
```

### Step 5: Access Application
```bash
# Port forward
kubectl port-forward svc/payment-gateway 8080:80 -n payment-gateway-dev

# Or access via ingress URL (if configured)
curl https://payment-gateway-dev.example.com/health
```

---

## Environment-Specific Deployment

### Development
```bash
# Create namespace
kubectl create namespace payment-gateway-dev

# Install with dev values
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-dev \
  -f values-dev.yaml \
  --set image.tag=latest

# Result: 1 replica, no auto-scale, debug enabled
```

### Staging
```bash
# Create namespace
kubectl create namespace payment-gateway-staging

# Install with staging values
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-staging \
  -f values-staging.yaml \
  --set image.tag=1.0.0

# Result: 2 replicas, auto-scale 2-5, production-like
```

### Production
```bash
# Create namespace
kubectl create namespace payment-gateway-prod

# Install with prod values
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set image.tag=1.0.0 \
  --set secrets.data.BankApiKey=$BANK_API_KEY \
  --set secrets.data.AppInsightsKey=$INSIGHTS_KEY

# Result: 3 replicas, auto-scale 3-20, all security enabled
```

---

## Configuration & Customization

### Override Values via Command Line
```bash
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set replicaCount=5 \
  --set image.tag=2.0.0 \
  --set ingress.hosts[0].host=api.example.com \
  --set resources.limits.memory=1Gi
```

### Create Custom Values File
```bash
# Copy and modify
cp values-prod.yaml custom-values.yaml

# Edit
nano custom-values.yaml

# Deploy
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-custom \
  -f custom-values.yaml
```

### Key Configuration Options

**Replicas and Scaling:**
```yaml
replicaCount: 3
autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 20
  targetCPUUtilizationPercentage: 70
```

**Resources:**
```yaml
resources:
  limits:
    cpu: 2000m
    memory: 1Gi
  requests:
    cpu: 1000m
    memory: 512Mi
```

**Ingress:**
```yaml
ingress:
  enabled: true
  hosts:
    - host: payment-gateway.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: payment-gateway-tls
      hosts:
        - payment-gateway.example.com
```

**Bank Configuration:**
```yaml
config:
  bank:
    authorizeEndpoint: https://bank-api.example.com/authorize
    timeoutSeconds: 15
    maxRetries: 2
    circuitBreakerThresholdFailures: 3
```

---

## Common Helm Commands

```bash
# Chart operations
helm install payment-gateway ./payment-gateway -n ns       # Install
helm upgrade payment-gateway ./payment-gateway -n ns       # Upgrade
helm uninstall payment-gateway -n ns                       # Delete
helm rollback payment-gateway 1 -n ns                      # Rollback

# Information
helm list -n ns                                             # List releases
helm status payment-gateway -n ns                          # Release status
helm get values payment-gateway -n ns                      # Show values
helm get manifest payment-gateway -n ns                    # Show manifests
helm history payment-gateway -n ns                         # Version history

# Validation
helm lint ./payment-gateway                                # Check chart
helm template ./payment-gateway -n ns -f values-prod.yaml  # Render templates
helm dry-run install payment-gateway ./payment-gateway     # Dry run

# Testing
helm test payment-gateway -n ns                            # Run tests
```

---

## Updating Configuration

### Update Values
```bash
# Edit values file
nano values-prod.yaml

# Upgrade release
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml
```

### Update Image
```bash
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set image.tag=2.0.0
```

### Scale Replicas
```bash
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set replicaCount=5
```

---

## Monitoring and Troubleshooting

### Check Deployment Status
```bash
kubectl rollout status deployment/payment-gateway -n payment-gateway-prod
kubectl get pods -n payment-gateway-prod
kubectl describe pod <pod-name> -n payment-gateway-prod
```

### View Logs
```bash
# Current logs
kubectl logs -f deployment/payment-gateway -n payment-gateway-prod

# Logs from specific pod
kubectl logs <pod-name> -n payment-gateway-prod

# Logs from all pods
kubectl logs -f -l app=payment-gateway -n payment-gateway-prod
```

### Check Services
```bash
kubectl get svc -n payment-gateway-prod
kubectl describe svc payment-gateway -n payment-gateway-prod
kubectl get endpoints -n payment-gateway-prod
```

### Health Check
```bash
# Port forward and test
kubectl port-forward svc/payment-gateway 8080:80 -n payment-gateway-prod
curl http://localhost:8080/health
```

### Debug Pod
```bash
# Interactive shell
kubectl exec -it <pod-name> -n payment-gateway-prod -- /bin/sh

# View environment variables
kubectl exec <pod-name> -n payment-gateway-prod -- env | grep ASPNETCORE
```

---

## Advanced Features

### Use Existing Secrets
```bash
# Create secret
kubectl create secret generic payment-gateway-secrets \
  --from-literal=BankApiKey=your-key \
  --from-literal=EncryptionKey=your-key \
  -n payment-gateway-prod

# Update values to reference
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set secrets.create=false \
  --set secrets.name=payment-gateway-secrets
```

### Configure Ingress with TLS
```yaml
ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
  hosts:
    - host: payment-gateway.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: payment-gateway-tls
      hosts:
        - payment-gateway.example.com
```

### Setup Prometheus Monitoring
```bash
# Install prometheus-community Helm chart
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install prometheus prometheus-community/kube-prometheus-stack \
  -n monitoring --create-namespace

# Enable ServiceMonitor in payment-gateway
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set monitoring.serviceMonitor.enabled=true
```

### Auto-scaling Configuration
```yaml
autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 20
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 50
          periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
        - type: Percent
          value: 100
          periodSeconds: 30
```

---

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Deploy to AKS

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Get AKS Credentials
        run: |
          az aks get-credentials \
            --resource-group payment-gateway-rg \
            --name payment-gateway-aks
      
      - name: Deploy with Helm
        run: |
          helm upgrade payment-gateway ./payment-gateway \
            -n payment-gateway-prod \
            -f values-prod.yaml \
            --install \
            --set image.tag=${{ github.sha }} \
            --wait
      
      - name: Verify Deployment
        run: |
          kubectl rollout status deployment/payment-gateway \
            -n payment-gateway-prod \
            --timeout=5m
```

---

## Backup and Disaster Recovery

### Backup Helm Release
```bash
# Get current values
helm get values payment-gateway -n payment-gateway-prod > backup-values.yaml

# Get manifest
helm get manifest payment-gateway -n payment-gateway-prod > backup-manifest.yaml

# Backup to git
git add backup-values.yaml
git commit -m "Backup helm release"
```

### Restore Helm Release
```bash
# Rollback to previous version
helm rollback payment-gateway -n payment-gateway-prod

# Or reinstall from backup
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f backup-values.yaml
```

---

## Security Checklist

- [ ] Use private container registry with authentication
- [ ] Enable Network Policies
- [ ] Configure RBAC with least privilege
- [ ] Use Secrets for sensitive data
- [ ] Enable Pod Security Policy
- [ ] Configure resource requests/limits
- [ ] Use read-only root filesystem
- [ ] Run as non-root user
- [ ] Enable network encryption (TLS)
- [ ] Regular security scanning
- [ ] Backup and disaster recovery plan

---

## Cost Optimization

### Development
- Minimal replicas: 1
- Small resource requests
- Auto-scaling disabled
- Local storage only

### Production
- Replicas: 3+
- Auto-scaling enabled
- Generous resource limits
- Monitoring enabled
- Estimated cost: $500-2000/month per environment

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Pod pending | Check node resources: `kubectl describe node` |
| CrashLoopBackOff | View logs: `kubectl logs <pod>` |
| ImagePullBackOff | Check registry credentials and image |
| Service not accessible | Check ingress, network policies, security groups |
| High memory usage | Increase limit: `--set resources.limits.memory=2Gi` |
| Slow scaling | Adjust HPA thresholds and metrics |

---

## Next Steps

1. **Customize values** for your environment
2. **Configure secrets** with real API keys
3. **Setup monitoring** with Prometheus/Grafana
4. **Configure logging** with ELK or similar
5. **Setup CI/CD** pipeline
6. **Test disaster recovery**
7. **Document** deployment process
8. **Train team** on Helm operations

---

## Resources

- **Helm Documentation:** https://helm.sh/docs/
- **Kubernetes Documentation:** https://kubernetes.io/docs/
- **AKS Documentation:** https://docs.microsoft.com/en-us/azure/aks/
- **Best Practices:** https://helm.sh/docs/chart_best_practices/

---

This Helm chart provides a complete, production-ready deployment for the Payment Gateway on Kubernetes!
