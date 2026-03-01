{{- /* _helpers.tpl - Template helpers */ -}}
{{/*
Expand the name of the chart.
*/}}
{{- define "payment-gateway.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "payment-gateway.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "payment-gateway.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "payment-gateway.labels" -}}
helm.sh/chart: {{ include "payment-gateway.chart" . }}
{{ include "payment-gateway.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
environment: {{ .Values.global.environment }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "payment-gateway.selectorLabels" -}}
app.kubernetes.io/name: {{ include "payment-gateway.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app: payment-gateway
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "payment-gateway.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "payment-gateway.fullname" . ) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

---
# service.yaml
apiVersion: v1
kind: Service
metadata:
  name: {{ include "payment-gateway.fullname" . }}
  namespace: {{ .Release.Namespace }}
  labels:
    {{- include "payment-gateway.labels" . | nindent 4 }}
  {{- with .Values.service.annotations }}
  annotations:
    {{- toYaml . | nindent 4 }}
  {{- end }}
spec:
  type: {{ .Values.service.type }}
  ports:
  - port: {{ .Values.service.port }}
    targetPort: http
    protocol: {{ .Values.service.protocol }}
    name: http
    {{- if .Values.service.nodePort }}
    nodePort: {{ .Values.service.nodePort }}
    {{- end }}
  selector:
    {{- include "payment-gateway.selectorLabels" . | nindent 4 }}
  {{- if .Values.service.loadBalancerIP }}
  loadBalancerIP: {{ .Values.service.loadBalancerIP }}
  {{- end }}
  {{- if .Values.service.loadBalancerSourceRanges }}
  loadBalancerSourceRanges:
    {{- toYaml .Values.service.loadBalancerSourceRanges | nindent 4 }}
  {{- end }}

---
# configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: {{ include "payment-gateway.fullname" . }}-config
  namespace: {{ .Release.Namespace }}
  labels:
    {{- include "payment-gateway.labels" . | nindent 4 }}
data:
  environment: {{ .Values.global.environment }}
  bank.authorizeEndpoint: {{ .Values.config.bank.authorizeEndpoint }}
  bank.timeoutSeconds: "{{ .Values.config.bank.timeoutSeconds }}"
  bank.maxRetries: "{{ .Values.config.bank.maxRetries }}"
  bank.retryDelayMilliseconds: "{{ .Values.config.bank.retryDelayMilliseconds }}"
  bank.circuitBreakerThresholdFailures: "{{ .Values.config.bank.circuitBreakerThresholdFailures }}"
  bank.circuitBreakerTimeoutSeconds: "{{ .Values.config.bank.circuitBreakerTimeoutSeconds }}"
  bank.bulkheadMaxParallelization: "{{ .Values.config.bank.bulkheadMaxParallelization }}"
  bank.bulkheadMaxQueueingActions: "{{ .Values.config.bank.bulkheadMaxQueueingActions }}"
  logging.level: {{ .Values.config.logging.level }}

---
# secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: {{ .Values.secrets.name }}
  namespace: {{ .Release.Namespace }}
  labels:
    {{- include "payment-gateway.labels" . | nindent 4 }}
type: Opaque
data:
  {{- if .Values.secrets.data.BankApiKey }}
  BankApiKey: {{ .Values.secrets.data.BankApiKey | b64enc }}
  {{- else }}
  BankApiKey: {{ "placeholder" | b64enc }}
  {{- end }}
  {{- if .Values.secrets.data.EncryptionKey }}
  EncryptionKey: {{ .Values.secrets.data.EncryptionKey | b64enc }}
  {{- else }}
  EncryptionKey: {{ "placeholder" | b64enc }}
  {{- end }}
  {{- if .Values.secrets.data.AppInsightsKey }}
  AppInsightsKey: {{ .Values.secrets.data.AppInsightsKey | b64enc }}
  {{- else }}
  AppInsightsKey: {{ "placeholder" | b64enc }}
  {{- end }}

---
# serviceaccount.yaml
{{- if .Values.serviceAccount.create -}}
apiVersion: v1
kind: ServiceAccount
metadata:
  name: {{ include "payment-gateway.serviceAccountName" . }}
  namespace: {{ .Release.Namespace }}
  labels:
    {{- include "payment-gateway.labels" . | nindent 4 }}
  {{- with .Values.serviceAccount.annotations }}
  annotations:
    {{- toYaml . | nindent 4 }}
  {{- end }}
{{- end }}

---
# hpa.yaml
{{- if .Values.autoscaling.enabled }}
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: {{ include "payment-gateway.fullname" . }}
  namespace: {{ .Release.Namespace }}
  labels:
    {{- include "payment-gateway.labels" . | nindent 4 }}
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: {{ include "payment-gateway.fullname" . }}
  minReplicas: {{ .Values.autoscaling.minReplicas }}
  maxReplicas: {{ .Values.autoscaling.maxReplicas }}
  metrics:
  {{- if .Values.autoscaling.targetCPUUtilizationPercentage }}
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: {{ .Values.autoscaling.targetCPUUtilizationPercentage }}
  {{- end }}
  {{- if .Values.autoscaling.targetMemoryUtilizationPercentage }}
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: {{ .Values.autoscaling.targetMemoryUtilizationPercentage }}
  {{- end }}
  {{- if .Values.autoscaling.behavior }}
  behavior:
    {{- toYaml .Values.autoscaling.behavior | nindent 4 }}
  {{- end }}
{{- end }}

---
# pdb.yaml
{{- if .Values.podDisruptionBudget.enabled }}
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: {{ include "payment-gateway.fullname" . }}
  namespace: {{ .Release.Namespace }}
  labels:
    {{- include "payment-gateway.labels" . | nindent 4 }}
spec:
  {{- if .Values.podDisruptionBudget.minAvailable }}
  minAvailable: {{ .Values.podDisruptionBudget.minAvailable }}
  {{- end }}
  selector:
    matchLabels:
      {{- include "payment-gateway.selectorLabels" . | nindent 6 }}
{{- end }}
