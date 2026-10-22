{{- define "agtec.labels" -}}
app.kubernetes.io/part-of: agtec-reference
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end -}}

{{- define "agtec.image" -}}
{{ .Values.image.registry }}/{{ .component }}:{{ .Values.image.tag }}
{{- end -}}

{{/* Shared env: transport selection + telemetry + connection strings from the referenced Secret. */}}
{{- define "agtec.env" -}}
- name: AGTec__Messaging__Provider
  value: {{ .Values.messagingProvider | quote }}
- name: AGTec__Search__Provider
  value: {{ .Values.searchProvider | quote }}
- name: AGTec__Secrets__Provider
  value: {{ .Values.secretsProvider | quote }}
- name: AGTec__Secrets__VaultUri
  value: {{ .Values.keyVaultUri | quote }}
- name: AGTec__Storage__Provider
  value: {{ .Values.storageProvider | quote }}
- name: AGTec__Storage__Scanner
  value: {{ .Values.scannerProvider | quote }}
- name: OTEL_EXPORTER_OTLP_ENDPOINT
  value: {{ .Values.otlpEndpoint | quote }}
- name: ConnectionStrings__forecasts
  valueFrom:
    secretKeyRef:
      name: {{ .Values.existingSecret }}
      key: ConnectionStrings__forecasts
- name: ConnectionStrings__messaging
  valueFrom:
    secretKeyRef:
      name: {{ .Values.existingSecret }}
      key: ConnectionStrings__messaging
- name: ConnectionStrings__search
  valueFrom:
    secretKeyRef:
      name: {{ .Values.existingSecret }}
      key: ConnectionStrings__search
- name: ConnectionStrings__blobs
  valueFrom:
    secretKeyRef:
      name: {{ .Values.existingSecret }}
      key: ConnectionStrings__blobs
{{- end -}}
