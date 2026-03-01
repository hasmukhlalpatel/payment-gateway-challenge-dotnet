# Helm Chart Quick Start - 10 Minutes

## Prerequisites

```bash
# Install Helm
brew install helm

# Login to AKS
az login
az aks get-credentials --resource-group my-rg --name my-aks
```

## Deploy in 3 Commands

### 1. Create Namespace
```bash
kubectl create namespace payment-gateway-prod
```

### 2. Install Chart
```bash
helm install payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set secrets.data.BankApiKey=$BANK_API_KEY \
  --set secrets.data.AppInsightsKey=$INSIGHTS_KEY
```

### 3. Verify
```bash
kubectl get all -n payment-gateway-prod
```

**Done!** 🎉

---

## Check Status

```bash
# Pod status
kubectl get pods -n payment-gateway-prod

# Service status
kubectl get svc -n payment-gateway-prod

# Deployment status
kubectl rollout status deployment/payment-gateway -n payment-gateway-prod
```

---

## View Logs

```bash
# Live logs
kubectl logs -f deployment/payment-gateway -n payment-gateway-prod

# Specific pod
kubectl logs <pod-name> -n payment-gateway-prod
```

---

## Port Forward & Test

```bash
# Port forward
kubectl port-forward svc/payment-gateway 8080:80 -n payment-gateway-prod

# In another terminal
curl http://localhost:8080/health
```

---

## Update Deployment

```bash
# Change image tag
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set image.tag=2.0.0

# Change replicas
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set replicaCount=5

# Change resources
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml \
  --set resources.limits.memory=1Gi
```

---

## Delete Release

```bash
helm uninstall payment-gateway -n payment-gateway-prod
```

---

## Common Issues

### Pods stuck in Pending
```bash
kubectl describe node
# Check available resources
```

### CrashLoopBackOff
```bash
kubectl logs <pod-name> -n payment-gateway-prod
# Check application logs for errors
```

### ImagePullBackOff
```bash
# Check registry credentials
kubectl get secret -n payment-gateway-prod
# Verify image exists in registry
```

---

## Files You Have

- **Chart.yaml** - Chart metadata
- **values.yaml** - Default configuration
- **values-dev.yaml** - Development overrides
- **values-staging.yaml** - Staging overrides
- **values-prod.yaml** - Production overrides
- **templates/** - Kubernetes manifests
  - deployment.yaml
  - service.yaml
  - ingress.yaml
  - configmap.yaml
  - secret.yaml
  - hpa.yaml (auto-scaling)
  - pdb.yaml (disruption budget)
  - networkpolicy.yaml (network security)

---

## Key Commands

```bash
helm list                                    # List releases
helm status payment-gateway                  # Release status
helm get values payment-gateway              # Show values
helm upgrade payment-gateway ./payment-gateway  # Update
helm rollback payment-gateway 1              # Rollback version
helm test payment-gateway                    # Run tests
helm lint ./payment-gateway                  # Validate chart
```

---

## Customization

Edit your environment file (values-prod.yaml):

```yaml
replicaCount: 5              # More replicas
resources:
  limits:
    memory: 1Gi              # More memory
autoscaling:
  maxReplicas: 30            # Larger auto-scale
ingress:
  hosts:
    - host: api.company.com  # Your domain
```

Then upgrade:
```bash
helm upgrade payment-gateway ./payment-gateway \
  -n payment-gateway-prod \
  -f values-prod.yaml
```

---

## Access Application

### Via Ingress
```bash
# If configured
https://payment-gateway.example.com
```

### Via Port Forward
```bash
kubectl port-forward svc/payment-gateway 8080:80
# http://localhost:8080
```

### Via LoadBalancer
```bash
kubectl get svc -n payment-gateway-prod
# Use EXTERNAL-IP if type: LoadBalancer
```

---

Done! Your Payment Gateway is now running on Kubernetes! 🚀
