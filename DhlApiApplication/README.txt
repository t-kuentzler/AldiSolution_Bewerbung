# DhlApiApplication

## Beschreibung
Das Projekt hat 2 Kernfunktionen:

1. Es werden alle Lieferung mit dem Status 'SHIPPED' und dem Carrier 'DHL' aus der Datenbank ausgelesen.
   Die Trackingcodes werden dann der DHL Api übermittelt, wo man den Status des Pakets zurück bekommt.
   Ist der Status 'delivered', wird der Status der Lieferung auf 'DELIVERED' in der Datenbank aktualisiert
   
2. Es werden alle Bestellungen mit dem Status 'SHIPPED' aus der Datenbank abgerufen.
   Für jede Bestellung wird geprüft, ob mindestens eine Lieferungen den Status 'DELIVERED' hat.
   Falls ja, wird der Status der gesamten Bestellung auf 'DELIVERED' aktualisiert