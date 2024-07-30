# DpdPushTrackingApi

## Beschreibung
Das Projekt dient als API um Trackingdaten von DPD zu empfangen.
Anstatt wie bei DHL immer Anfragen zu senden, werden die Daten von DPD per PUSH bei änderungen eines Paketstatus an unseren API Server gesendet.
Ist der Status 'customer_delivery', wird der Status der Lieferung auf 'DELIVERED' in der Datenbank aktualisiert
